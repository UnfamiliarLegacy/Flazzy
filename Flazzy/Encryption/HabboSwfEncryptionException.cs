// Copyright (c) 2022, UnfamiliarLegacy. All rights reserved.
// Licensed under the GNU General Public License v3.0.
// Author https://github.com/UnfamiliarLegacy

using System.Runtime.Serialization;

namespace Flazzy.Encryption;

public class HabboSwfEncryptionException : Exception
{
    public HabboSwfEncryptionException()
    {
    }

    protected HabboSwfEncryptionException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public HabboSwfEncryptionException(string message) : base(message)
    {
    }

    public HabboSwfEncryptionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}