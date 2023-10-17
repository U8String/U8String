using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace U8Primitives;

internal static class U8Interning
{
    static readonly bool _useEncodedPool = !(AppContext
        .TryGetSwitch("U8String.DisableEncodedPool", out var disable) && disable);
    static readonly bool _useDecodedPool = !(AppContext
        .TryGetSwitch("U8String.DisableDecodedPool", out var disable) && disable);

    // Unconditionally enable for NativeAOT. There is an escape hatch in the form of
    // setting U8String.EncodedPoolThreshold to a lower value or zero.
    static bool UseEncodedPool => !RuntimeFeature.IsDynamicCodeCompiled || _useEncodedPool;
    static bool UseDecodedPool => !RuntimeFeature.IsDynamicCodeCompiled || _useDecodedPool;

    static readonly int EncodedPoolThreshold = AppContext
        .GetData("U8String.EncodedPoolThreshold") is int threshold ? threshold : 32768;
    static readonly int DecodedPoolThreshold = AppContext
        .GetData("U8String.DecodedPoolThreshold") is int threshold ? threshold : 32768;

    // Contract: only not-empty strings are interned.
    static readonly ConditionalWeakTable<string, byte[]> EncodedPool = [];
    static readonly ConditionalWeakTable<byte[], ConcurrentDictionary<long, string>> DecodedPool = [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetEncoded(string value, out U8String encoded)
    {
        Debug.Assert(!string.IsNullOrEmpty(value));

        if (UseEncodedPool)
        {
            encoded = GetEncoded(value);
            return true;
        }

        Unsafe.SkipInit(out encoded);
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static U8String GetEncoded(string value)
    {
        // This does not really coalesce the cctor checks for NativeAOT, but at least
        // it puts both of them in one place, helping the branch predictor.
        var (pool, threshold) = (EncodedPool, EncodedPoolThreshold);

        if (pool.TryGetValue(value, out var encoded))
        {
            Debug.Assert(encoded.Length > 0);
            return new(encoded, 0, encoded.Length - 1);
        }

        var length = Encoding.UTF8.GetByteCount(value);
        encoded = new byte[length + 1];
        Encoding.UTF8.GetBytes(value, encoded);

        if (length < threshold)
        {
            pool.AddOrUpdate(value, encoded);
        }

        return new(encoded, 0, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetDecoded(U8String value, out string decoded)
    {
        Debug.Assert(!value.IsEmpty);

        if (UseDecodedPool)
        {
            decoded = GetDecoded(value);
            return true;
        }

        Unsafe.SkipInit(out decoded);
        return false;
    }

    static string GetDecoded(U8String value)
    {
        Debug.Assert(value.Length > 0);
        Debug.Assert(Unsafe.SizeOf<U8Range>() is sizeof(long));

        var (pool, threshold) = (DecodedPool, DecodedPoolThreshold);

        var source = value._value;
        var range = Unsafe.BitCast<U8Range, long>(value._inner);

        Debug.Assert(source != null);

        string decoded;
        if (pool.TryGetValue(source, out var decodedMap))
        {
            if (decodedMap.TryGetValue(range, out var interned))
            {
                return interned;
            }
            
            decoded = Encoding.UTF8.GetString(value.UnsafeSpan);

            if (decoded.Length < threshold)
            {
                decodedMap[range] = decoded;
            }

            return decoded;
        }

        decoded = Encoding.UTF8.GetString(value.UnsafeSpan);

        if (decoded.Length < threshold)
        {
            pool.AddOrUpdate(source, new() { [range] = decoded });
        }

        return decoded;
    }
}
