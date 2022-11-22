namespace Flazzy;

public enum CompressionKind
{
    /// <summary>
    /// Represents no compression.
    /// </summary>
    None = 0x46,
    /// <summary>
    /// Represents ZLIB compression. (SWF +6)
    /// </summary>
    ZLIB = 0x43,
    /// <summary>
    /// Represents ZLIB compression, encrypted.
    /// </summary>
    ZLIB_Encrypted = 0x63,
    /// <summary>
    /// Represents LZMA compression. (SWF +13)
    /// </summary>
    LZMA = 0x5A,
    /// <summary>
    /// Represents LZMA compression, encrypted.
    /// </summary>
    LZMA_Encrypted = 0x7A
}
