// Copyright (c) 2022, UnfamiliarLegacy. All rights reserved.
// Licensed under the GNU General Public License v3.0.
// Author https://github.com/UnfamiliarLegacy

namespace Flazzy.Encryption;

public readonly ref struct KeyParams
{
    public required byte[] Key { get; init; }
    public required byte[] IV { get; init; }
    public required int LengthOriginal { get; init; }
    public required int LengthEncrypted { get; init; }
}

public interface IHabboSwfKeyDeriver
{
    KeyParams CreateDecryptionKey(Span<byte> data);
}