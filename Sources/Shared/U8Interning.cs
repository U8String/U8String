using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Unicode;

using U8.Primitives;

namespace U8.Shared;

// Contract: only not-empty strings are interned.
static class U8Interning
{
    static bool UseEncodedPool { get; } = RuntimeFeature.IsDynamicCodeCompiled && AppContext
        .TryGetSwitch("U8String.UseEncodedPool", out var enabled) && enabled;
    static bool UseDecodedPool { get; } = RuntimeFeature.IsDynamicCodeCompiled && AppContext
        .TryGetSwitch("U8String.UseDecodedPool", out var enabled) && enabled;

    // Separate class to allow NativeAOT completely trim away the interning code
    // by ensuring there is no cctor check in Use(Encoded/Decoded)Pool properties.
    static class InternPool
    {
        internal static readonly ConditionalWeakTable<string, byte[]> Encoded = [];
        internal static readonly ConditionalWeakTable<byte[], ConcurrentDictionary<long, string>> Decoded = [];

        internal static readonly int EncodeThreshold = AppContext
            .GetData("U8String.EncodedPoolThreshold") is int threshold ? threshold : 8192 * 1024;
        internal static readonly int DecodeThreshold = AppContext
            .GetData("U8String.DecodedPoolThreshold") is int threshold ? threshold : 8192 * 1024;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetEncoded(string value, out U8String encoded)
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

    internal static U8String GetEncoded(string value)
    {
        // This does not really coalesce the cctor checks for NativeAOT, but at least
        // it puts both of them in one place, helping the branch predictor.
        var (pool, threshold) = (InternPool.Encoded, InternPool.EncodeThreshold);

        if (pool.TryGetValue(value, out var encoded))
        {
            Debug.Assert(encoded.Length > 0);
            return new(encoded, 0, encoded.Length - 1);
        }

        var length = Encoding.UTF8.GetByteCount(value);
        encoded = new byte[length + 1];

         var result = Utf8.FromUtf16(
            source: value,
            destination: encoded,
            charsRead: out _,
            bytesWritten: out length,
            replaceInvalidSequences: false,
            isFinalBlock: true);

        if (result != OperationStatus.Done)
        {
            ThrowHelpers.InvalidUtf8();
        }

        if (length < threshold)
        {
            pool.AddOrUpdate(value, encoded);
        }

        return new(encoded, 0, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryGetDecoded(U8String value, out string decoded)
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

    internal static string GetDecoded(U8String value)
    {
        Debug.Assert(value.Length > 0);
        Debug.Assert(Unsafe.SizeOf<U8Range>() is sizeof(long));

        var (pool, threshold) = (InternPool.Decoded, InternPool.DecodeThreshold);

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
