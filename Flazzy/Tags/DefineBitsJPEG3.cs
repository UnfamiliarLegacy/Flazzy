using System.Buffers;
using System.Buffers.Binary;
using Flazzy.Compression;
using Flazzy.IO;
using Flazzy.Records;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace Flazzy.Tags
{
    public class DefineBitsJPEG3 : TagItem, IImageTag
    {
        public DefineBitsJPEG3() : base(TagKind.DefineBitsJPEG3)
        {
        }
        
        public DefineBitsJPEG3(HeaderRecord header, FlashReader input) : base(header)
        {
            Id = input.ReadUInt16();

            var alphaDataOffset = input.ReadInt32();
            
            Data = input.ReadBytes(alphaDataOffset);
            Format = GetFormat(Data);
            
            if (Format == ImageFormat.JPEG)
            {
                var partialLength = 2 + 4 + alphaDataOffset;
                AlphaData = input.ReadBytes(Header.Length - partialLength);
            }
            else
            {
                // Minimum Compressed Empty Data Length
                AlphaData = input.ReadBytes(8);
            }
        }
        
        public ushort Id { get; set; }
        public ImageFormat Format { get; private set; }
        public byte[] Data { get; private set; }
        public byte[] AlphaData { get; private set; }
        
        public void SetImage(Image<Rgba32> image)
        {
            // Save image.
            using var stream = new MemoryStream();
            
            image.Save(stream, new JpegEncoder
            {
                Quality = 100
            });

            Data = stream.ToArray();
            Format = ImageFormat.JPEG;
            
            // Save alpha channel.
            var alpha = ArrayPool<byte>.Shared.Rent(image.Width * image.Height);
            
            image.ProcessPixelRows(pixelAccessor =>
            {
                for (var y = 0; y < pixelAccessor.Height; y++)
                {
                    var offset = pixelAccessor.Width * y;
                    var row = pixelAccessor.GetRowSpan(y);

                    for (var x = 0; x < pixelAccessor.Width; x++)
                    {
                        alpha[offset + x] = row[x].A;
                    }
                }
            });
            
            AlphaData = ZLIB.Compress(alpha);
        }

        public Image<Rgba32> GetImage(Configuration configuration = null)
        {
            if (Format != ImageFormat.JPEG)
            {
                throw new NotSupportedException("Only JPEG is supported for GetImage");
            }

            // Load image.
            var image = Image.Load<Rgba32>(new DecoderOptions
            {
                Configuration = configuration
            }, Data);
            
            // Apply alpha channel.
            var alphaSize = image.Width * image.Height;
            var alpha = ArrayPool<byte>.Shared.Rent(alphaSize);

            try
            {
                ZLIB.DecompressFast(AlphaData, alpha, alphaSize);
            
                image.ProcessPixelRows(pixelAccessor =>
                {
                    for (var y = 0; y < pixelAccessor.Height; y++)
                    {
                        var offset = pixelAccessor.Width * y;
                        var row = pixelAccessor.GetRowSpan(y);

                        for (var x = 0; x < pixelAccessor.Width; x++)
                        {
                            row[x].A = alpha[offset + x];
                        }
                    }
                });

                return image;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(alpha);
            }
        }
        
        private static ImageFormat GetFormat(ReadOnlySpan<byte> data)
        {
            if (BinaryPrimitives.ReadInt32LittleEndian(data) == -654321153 || 
                BinaryPrimitives.ReadInt16LittleEndian(data) == -9985)
            {
                return ImageFormat.JPEG;
            }

            if (BinaryPrimitives.ReadInt64LittleEndian(data) == 727905341920923785)
            {
                return ImageFormat.PNG;
            }

            if (BinaryPrimitives.ReadInt32LittleEndian(data) == 944130375 && 
                BinaryPrimitives.ReadInt16LittleEndian(data.Slice(4)) == 24889)
            {
                return ImageFormat.GIF98a;
            }
            
            throw new ArgumentException("Provided data contains an unknown image format.");
        }

        public override int GetBodySize()
        {
            var size = 0;
            size += sizeof(ushort);
            size += sizeof(uint);
            size += Data.Length;
            size += AlphaData.Length;
            return size;
        }
        
        protected override void WriteBodyTo(FlashWriter output)
        {
            output.Write(Id);
            output.Write((uint)Data.Length);
            output.Write(Data);
            output.Write(AlphaData);
        }
    }
}