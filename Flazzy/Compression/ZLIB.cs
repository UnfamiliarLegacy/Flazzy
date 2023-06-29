using System.IO.Compression;
using Flazzy.IO;

namespace Flazzy.Compression;

public static class ZLIB
{
    public static byte[] Compress(byte[] data)
    {
        using (var output = new MemoryStream())
        using (var compressor = new ZLibStream(output, CompressionLevel.SmallestSize))
        {
            compressor.Write(data);
            return output.ToArray();
        }
    }
    
    public static FlashReader WrapDecompressor(Stream input, bool leaveOpen = false)
    {
        return new FlashReader(new ZLibStream(input, CompressionMode.Decompress, leaveOpen));
    }
    
    public static FlashWriter WrapCompressor(Stream output, bool leaveOpen = false)
    {
        return new FlashWriter(new ZLibStream(output, CompressionLevel.SmallestSize, leaveOpen));
    }
        
    public static unsafe void DecompressFast(ReadOnlySpan<byte> source, byte[] destination, int decompressedSize) {
        fixed (byte* pBuffer = &source[0]) {
            using (var stream = new UnmanagedMemoryStream(pBuffer, source.Length)) 
            using (var deflateStream = new ZLibStream(stream, CompressionMode.Decompress)) {
                var read = deflateStream.Read(destination, 0, decompressedSize);
                if (read != decompressedSize)
                {
                    throw new Exception($"Decompressed incorrect amount of bytes ({read} != {decompressedSize})");
                }
            }
        }
    }
}