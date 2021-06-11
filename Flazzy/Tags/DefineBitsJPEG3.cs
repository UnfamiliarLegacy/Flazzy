using System;
using System.Drawing;
using System.IO;
using Flazzy.Compression;
using Flazzy.IO;
using Flazzy.Records;

namespace Flazzy.Tags
{
    public class DefineBitsJPEG3 : ImageTag
    {
        public ushort Id { get; set; }
        public byte[] Data { get; set; }
        public byte[] AlphaData { get; set; }

        public DefineBitsJPEG3()
            : base(TagKind.DefineBitsJPEG3)
        { }
        public DefineBitsJPEG3(HeaderRecord header, FlashReader input)
            : base(header)
        {
            Id = input.ReadUInt16();

            int alphaDataOffset = input.ReadInt32();
            Data = input.ReadBytes(alphaDataOffset);

            Format = GetFormat(Data);
            if (Format == ImageFormat.JPEG)
            {
                int partialLength = (2 + 4 + alphaDataOffset);
                AlphaData = input.ReadBytes(Header.Length - partialLength);
            }
            else
            {
                // Minimum Compressed Empty Data Length
                AlphaData = input.ReadBytes(8);
            }
        }

        public override Color[,] GetARGBMap()
        {
            using (var stream = new MemoryStream(Data))
            using (var bitmap = new Bitmap(stream))
            {
                var alphaChannel = ZLIB.Decompress(AlphaData);
                var colormap = new Color[bitmap.Width, bitmap.Height];

                for (var x = 0; x < bitmap.Width; x++)
                {
                    for (var y = 0; y < bitmap.Height; y++)
                    {
                        var pixel = bitmap.GetPixel(x, y);
                        var alpha = alphaChannel[(bitmap.Width * y) + x];
                        
                        colormap[x, y] = Color.FromArgb(alpha, pixel.R, pixel.G, pixel.B);
                    }
                }

                return colormap;
            }
        }
        public override void SetARGBMap(Color[,] map)
        {
            throw new NotSupportedException();
        }

        public override int GetBodySize()
        {
            int size = 0;
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