using System;
using System.IO;
using Flazzy.IO;
using Ionic.Zlib;

namespace Flazzy.Compression
{
    public static class ZLIB
    {
        public static byte[] Compress(byte[] data)
        {
            using (var output = new MemoryStream())
            using (var compressor = new ZlibStream(output, CompressionMode.Compress, CompressionLevel.BestCompression))
            {
                ZlibBaseStream.CompressBuffer(data, compressor);
                return output.ToArray();
            }
        }
        public static byte[] Decompress(byte[] data)
        {
            using (var input = new MemoryStream(data))
            using (var decompressor = new ZlibStream(input, CompressionMode.Decompress))
            {
                return ZlibBaseStream.UncompressBuffer(decompressor);
            }
        }

        public static FlashReader WrapDecompressor(Stream input, bool leaveOpen = false)
        {
            return new FlashReader(new ZlibStream(input, CompressionMode.Decompress, leaveOpen));
        }
        public static FlashWriter WrapCompressor(Stream output, bool leaveOpen = false)
        {
            return new FlashWriter(new ZlibStream(output, CompressionMode.Compress, CompressionLevel.BestCompression, leaveOpen));
        }
        
        public static unsafe void DecompressFast(ReadOnlySpan<byte> source, Span<byte> destination, int decompressedSize) {
            fixed (byte* pBuffer = &source[0]) {
                using (var stream = new UnmanagedMemoryStream(pBuffer, source.Length)) 
                using (var deflateStream = new ZlibStream(stream, CompressionMode.Decompress)) {
                    var read = deflateStream.Read(destination);
                    if (read != decompressedSize)
                    {
                        throw new Exception($"Decompressed incorrect amount of bytes ({read} != {decompressedSize})");
                    }
                }
            }
        }
    }
}