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

            if (Format == 3)
            {
                ColorTableSize = input.ReadByte();
            }

            var partialLength = 7 + (Format == 3 ? 1 : 0);
            _zlibData = input.ReadBytes(header.Length - partialLength);
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
                        var pixel = y * (Width * 4) + (x * 4);

                        data[pixel + 0] = row[x].A;
                        data[pixel + 1] = row[x].R;
                        data[pixel + 2] = row[x].G;
                        data[pixel + 3] = row[x].B;
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
            var decompressedData = ZLIB.Decompress(_zlibData);
            var image = new Image<Rgba32>(Width, Height);

            switch (Format)
            {
                // ARGB Format
                case 5:
                    for (var y = 0; y < image.Height; y++)
                    {
                        var row = image.GetPixelRowSpan(y);
                        for (var x = 0; x < image.Width; x++)
                        {
                            var pixel = y * (Width * 4) + (x * 4);
                            
                            row[x] = new Rgba32(
                                decompressedData[pixel + 1],
                                decompressedData[pixel + 2],
                                decompressedData[pixel + 3],
                                decompressedData[pixel]);
                        }
                    }
                    break;
                default:
                    image.Dispose();
                    throw new NotSupportedException($"Unsupported losless format {Format}");
            }
            
            return image;
        }

        public override int GetBodySize()
        {
            int size = 0;
            size += sizeof(ushort);
            size += sizeof(byte);
            size += sizeof(ushort);
            size += sizeof(ushort);
            if (Format == 3)
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
            
            if (Format == 3)
            {
                output.Write(ColorTableSize);
            }
            
            output.Write(_zlibData);
        }
    }
}