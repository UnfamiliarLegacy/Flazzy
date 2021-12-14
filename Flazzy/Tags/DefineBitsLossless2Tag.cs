using System;
using System.Buffers;
using Flazzy.IO;
using Flazzy.Records;
using Flazzy.Compression;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Flazzy.Tags
{
    public class DefineBitsLossless2Tag : TagItem, IImageTag
    {
        private const int Format8BitColormapped = 3;
        private const int Format32BitArgb = 5;
        
        private byte[] _zlibData;
        
        public DefineBitsLossless2Tag() : base(TagKind.DefineBitsLossless2)
        {
            _zlibData = Array.Empty<byte>();
        }
        
        public DefineBitsLossless2Tag(HeaderRecord header, FlashReader input) : base(header) {
            Id = input.ReadUInt16();
            Format = input.ReadByte();
            Width = input.ReadUInt16();
            Height = input.ReadUInt16();

            if (Format == Format8BitColormapped)
            {
                ColorTableSize = input.ReadByte();
            }

            var readSize = 7 + (Format == Format8BitColormapped ? 1 : 0);
            _zlibData = input.ReadBytes(header.Length - readSize);
        }
        
        public ushort Id { get; set; }
        public byte Format { get; private set; }
        public byte ColorTableSize { get; }
        public ushort Width { get; private set; }
        public ushort Height { get; private set; }

        public void SetImage(Image<Rgba32> image)
        {
            Format = 5;
            Width = (ushort)image.Width;
            Height = (ushort)image.Height;

            var data = ArrayPool<byte>.Shared.Rent(Width * Height * 4);

            try
            {
                for (var y = 0; y < image.Height; y++)
                {
                    var row = image.GetPixelRowSpan(y);
                    for (var x = 0; x < image.Width; x++)
                    {
                        var pixel = y * Width * 4 + x * 4;
                        
                        // Premultiply alpha.
                        var alpha = row[x].A;
                        var alphaChange = alpha / 255.0d;
                        
                        data[pixel + 0] = alpha;
                        data[pixel + 1] = (byte)(row[x].R * alphaChange);
                        data[pixel + 2] = (byte)(row[x].G * alphaChange);
                        data[pixel + 3] = (byte)(row[x].B * alphaChange);
                    }
                }

                _zlibData = ZLIB.Compress(data);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(data);
            }
        }

        public Image<Rgba32> GetImage()
        {
            var decompressedSize = Width * Height * 4;
            var decompressedData = ArrayPool<byte>.Shared.Rent(decompressedSize);

            try
            {
                ZLIB.DecompressFast(_zlibData, decompressedData, decompressedSize);
                
                var image = new Image<Rgba32>(Width, Height);

                switch (Format)
                {
                    case Format32BitArgb:
                        for (var y = 0; y < image.Height; y++)
                        {
                            var row = image.GetPixelRowSpan(y);
                            for (var x = 0; x < image.Width; x++)
                            {
                                // Alpha values are premultiplied, recover original values.
                                var pixel = y * Width * 4 + x * 4;
                                var alpha = decompressedData[pixel];
                                if (alpha != 0)
                                {
                                    var alphaChange = 255.0d / alpha;
                                
                                    row[x] = new Rgba32(
                                        (byte)(decompressedData[pixel + 1] * alphaChange),
                                        (byte)(decompressedData[pixel + 2] * alphaChange),
                                        (byte)(decompressedData[pixel + 3] * alphaChange),
                                        alpha);
                                }
                                else
                                {
                                    row[x] = new Rgba32(
                                        decompressedData[pixel + 1],
                                        decompressedData[pixel + 2],
                                        decompressedData[pixel + 3],
                                        alpha);
                                }
                            }
                        }
                        break;
                    default:
                        image.Dispose();
                        throw new NotSupportedException($"Unsupported losless format {Format}");
                }
            
                return image;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(decompressedData);
            }
        }

        public override int GetBodySize()
        {
            var size = 0;
            size += sizeof(ushort);
            size += sizeof(byte);
            size += sizeof(ushort);
            size += sizeof(ushort);
            
            if (Format == Format8BitColormapped)
            {
                size += sizeof(byte);
            }
            
            size += _zlibData.Length;
            return size;
        }
        
        protected override void WriteBodyTo(FlashWriter output)
        {
            output.Write(Id);
            output.Write(Format);
            output.Write(Width);
            output.Write(Height);
            
            if (Format == Format8BitColormapped)
            {
                output.Write(ColorTableSize);
            }
            
            output.Write(_zlibData);
        }
    }
}