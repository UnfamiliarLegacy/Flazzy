// Copyright (c) 2022, UnfamiliarLegacy. All rights reserved.
// Licensed under the GNU General Public License v3.0.
// Author https://github.com/UnfamiliarLegacy

using System.Buffers;
using System.Security.Cryptography;
using Flazzy.Encryption.Streams;

namespace Flazzy.Encryption;

public class HabboSwfEncryption
{
    private const int OffsetEncryptedLength = 8;
    private const int OffsetEncryptedData = 12;

    private readonly IHabboSwfKeyDeriver _keyDeriver;

    public HabboSwfEncryption(IHabboSwfKeyDeriver keyDeriver)
    {
        _keyDeriver = keyDeriver;
    }
    
    /// <summary>
    ///     Decrypts an SWF file and replaces it with the decrypted data.
    /// </summary>
    /// <param name="data">An encrypted SWF file.</param>
    /// <returns>
    ///     The total length of the decrypted SWF file, this will be less than <code>data.Length</code>.
    ///     The decrypted bytes are written to <see cref="data"/>.</returns>
    /// <exception cref="HabboSwfEncryptionException"></exception>
    public int Decrypt(Span<byte> data)
    {
        if (data[0] != 'z' && data[0] != 'c')
        {
            throw new HabboSwfEncryptionException("Invalid SWF header for decryption.");
        }
        
        // Restore decrypted SWF format.
        data[0] = (byte)(data[0] - 0x20);
        
        // Retrieve key parameters.
        var kParams = _keyDeriver.CreateDecryptionKey(data);
        
        // Decrypt body.
        using var aes = Aes.Create();

        aes.Mode = CipherMode.CBC;
        aes.KeySize = 256;
        aes.BlockSize = 128;
        aes.Key = kParams.Key;
        aes.IV = kParams.IV;

        if (!aes.TryDecryptCbc(data.Slice(OffsetEncryptedData, kParams.LengthEncrypted), kParams.IV, data.Slice(OffsetEncryptedLength), out _, PaddingMode.None))
        {
            throw new HabboSwfEncryptionException("Failed to decrypt swf body.");
        }
        
        return kParams.LengthOriginal;
    }

    public Stream Decrypt(Stream stream)
    {
        if (!stream.CanSeek)
        {
            throw new HabboSwfEncryptionException("Seeking is not supported by the stream to decrypt.");
        }

        stream.Seek(0, SeekOrigin.Begin);

        var fileLen = (int)stream.Length;
        var fileBuffer = ArrayPool<byte>.Shared.Rent(fileLen);

        try
        {
            if (stream.Read(fileBuffer, 0, fileLen) != stream.Length)
            {
                throw new HabboSwfEncryptionException("Failed to read entire stream.");
            }
            
            var decryptedLength = Decrypt(fileBuffer.AsSpan(0, fileLen));

            return new ArrayPoolStream(fileBuffer, 0, decryptedLength);
        }
        catch (Exception)
        {
            // Hopefully this is safe enough!
            // Should not cause leaks if the ArrayPoolStream is disposed properly.
            ArrayPool<byte>.Shared.Return(fileBuffer);
            throw;
        }
    }
}