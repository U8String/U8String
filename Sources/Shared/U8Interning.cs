using System.Diagnostics;
using System.Text;

namespace U8Primitives;

internal static class U8Interning
{
    static readonly bool UseEncodedPool = !(AppContext.TryGetSwitch("U8String.DisableEncodedPool", out var disabled) && disabled);
    static readonly int EncodedPoolThreshold = AppContext
        .GetData("U8String.EncodedPoolThreshold") is int threshold ? threshold : 8192 * 1024; // 8 MB

    // Contract: only not-empty strings are interned.
    static readonly ConditionalWeakTable<string, byte[]> EncodedPool = [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetEncoded(string value, out U8String encoded)
    {
        Debug.Assert(!string.IsNullOrEmpty(value));

        if (!UseEncodedPool)
        {
            encoded = default;
            return false;
        }

        encoded = GetEncoded(value);
        return true;
    }

    static U8String GetEncoded(string value)
    {
        if (EncodedPool.TryGetValue(value, out var encoded))
        {
            Debug.Assert(encoded.Length > 0);
            return new(encoded, 0, encoded.Length - 1);
        }

        var length = Encoding.UTF8.GetByteCount(value);
        encoded = new byte[length + 1];
        Encoding.UTF8.GetBytes(value, encoded);

        if (length < EncodedPoolThreshold)
        {
            EncodedPool.AddOrUpdate(value, encoded);
        }

        return new(encoded, 0, length);
    }
}
