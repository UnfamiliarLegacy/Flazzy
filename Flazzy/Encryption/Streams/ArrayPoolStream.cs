// Copyright (c) 2022, UnfamiliarLegacy. All rights reserved.
// Licensed under the GNU General Public License v3.0.
// Author https://github.com/UnfamiliarLegacy

using System.Buffers;

namespace Flazzy.Encryption.Streams;

internal class ArrayPoolStream : MemoryStream
{
    private readonly byte[] _buffer;

    public ArrayPoolStream(byte[] buffer, int index, int count) : base(buffer, index, count, false, false)
    {
        _buffer = buffer;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            ArrayPool<byte>.Shared.Return(_buffer);
        }
    }
}