using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Text;

namespace U8Primitives;

// TODO: Better name?
internal static class U8Searching
{
    /// <summary>
    /// Returns the index of the first occurrence of a specified value in a span.
    /// </summary>
    /// <remarks>
    /// Designed to be inlined into the caller and optimized away on constants.
    /// <para>
    /// Contract: when T is char and a surrogate, the return value is false.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool Contains<T>(ReadOnlySpan<byte> value, T item)
        where T : struct
    {
        Debug.Assert(item is byte or char or Rune or U8String);

        return item switch
        {
            byte b => value.Contains(b),

            char c => char.IsAscii(c)
                ? value.Contains((byte)c)
                : !char.IsSurrogate(c)
                    && value.IndexOf(U8Scalar.Create(c, checkAscii: false).AsSpan()) >= 0,

            Rune r => r.IsAscii
                ? value.Contains((byte)r.Value)
                : value.IndexOf(U8Scalar.Create(r, checkAscii: false).AsSpan()) >= 0,

            U8String str => value.IndexOf(str) >= 0,

            _ => ThrowHelpers.Unreachable<bool>()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool Contains(ReadOnlySpan<byte> value, ReadOnlySpan<byte> item)
    {
        return item.Length is 1 ? value.Contains(item.AsRef()) : value.IndexOf(item) >= 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool SplitContains<T>(
        ReadOnlySpan<byte> value,
        T separator,
        ReadOnlySpan<byte> item) where T : struct
    {
        return !Contains(item, separator) && Contains(value, item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool SplitContains(
        ReadOnlySpan<byte> value,
        ReadOnlySpan<byte> separator,
        ReadOnlySpan<byte> item)
    {
        // When the item we are looking for contains the separator, it means that it will
        // never be found in the split since it would be pointing to the split boundary.
        return !Contains(item, separator) && Contains(value, item);
    }

    /// <summary>
    /// Contract: when T is char, it must never be a surrogate.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Count<T>(U8String value, T item)
        where T : struct
    {
        Debug.Assert(!value.IsEmpty);
        Debug.Assert(item is not char i || !char.IsSurrogate(i));
        Debug.Assert(item is byte or char or Rune or U8String);

        return item switch
        {
            byte b => value.UnsafeSpan.Count(b),

            char c => char.IsAscii(c)
                ? value.UnsafeSpan.Count((byte)c)
                : value.UnsafeSpan.Count(U8Scalar.Create(c, checkAscii: false).AsSpan()),

            Rune r => r.IsAscii
                ? value.UnsafeSpan.Count((byte)r.Value)
                : value.UnsafeSpan.Count(U8Scalar.Create(r, checkAscii: false).AsSpan()),

            U8String str => value.UnsafeSpan.Count(str),

            _ => ThrowHelpers.Unreachable<int>()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Count(ReadOnlySpan<byte> value, ReadOnlySpan<byte> item)
    {
        //return item.Length is 1 ? value.Count(item.AsRef()) : value.Count(item);
        return value.Count(item); // This already has internal check for Length is 1
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Count(ReadOnlySpan<byte> value, ReadOnlySpan<byte> item, U8SplitOptions options)
    {
        if (options is U8SplitOptions.None)
        {
            return Count(value, item);
        }

        return CountSlow(value, item, options);
    }

    internal static int Count<T>(ReadOnlySpan<byte> value, T item, U8SplitOptions options)
    {
        Debug.Assert(options != U8SplitOptions.None);
        throw new NotImplementedException();
    }

    internal static int CountSlow(ReadOnlySpan<byte> value, ReadOnlySpan<byte> item, U8SplitOptions options)
    {
        Debug.Assert(options != U8SplitOptions.None);
        throw new NotImplementedException();
    }

    // TODO: Count without cast -> lt -> sub vec len?
    // TODO 2: Consider adding AVX512 path?
    internal static int CountRunes(ref byte src, nuint length)
    {
        // Adopted from https://github.com/simdutf/simdutf/blob/master/src/generic/utf8.h#L10
        var count = 0;
        var offset = (nuint)0;
        ref var ptr = ref Unsafe.As<byte, sbyte>(ref src);

        if (length >= (nuint)Vector256<byte>.Count)
        {
            var continuations = Vector256.Create((sbyte)-64);
            var lastvec = length - (nuint)Vector256<byte>.Count;
            do
            {
                var chunk = Vector256.LoadUnsafe(ref ptr.Add(offset));
                var matches = Vector256.LessThan(chunk, continuations);

                count += 32 - matches.AsByte().CountMatches();
                offset += (nuint)Vector256<byte>.Count;
            } while (offset <= lastvec);
        }

        if (offset <= length - (nuint)Vector128<byte>.Count)
        {
            var continuations = Vector128.Create((sbyte)-64);
            var chunk = Vector128.LoadUnsafe(ref ptr.Add(offset));
            var matches = Vector128.LessThan(chunk, continuations);

            count += 16 - matches.AsByte().CountMatches();
            offset += (nuint)Vector128<byte>.Count;
        }

        if (AdvSimd.IsSupported &&
            (offset <= length - (nuint)Vector64<byte>.Count))
        {
            var continuations = Vector64.Create((sbyte)-64);
            var chunk = Vector64.LoadUnsafe(ref ptr.Add(offset));
            var matches = Vector64
                .LessThan(chunk, continuations)
                .AsUInt64()
                .ToScalar();

            count += 8 - (BitOperations.PopCount(matches) / 8);
            offset += (nuint)Vector64<byte>.Count;
        }

        while (offset < length)
        {
            // Branchless: x86_64: cmp + setge; arm64: cmn + cset
            count += U8Info.IsContinuationByte((byte)ptr.Add(offset)) ? 0 : 1;
            offset++;
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int IndexOf<T>(ReadOnlySpan<byte> value, T item)
        where T : struct
    {
        return IndexOf(value, item, out _);
    }

    /// <summary>
    /// Contract: when T is char, it must never be a surrogate.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int IndexOf<T>(ReadOnlySpan<byte> value, T item, out int size)
        where T : struct
    {
        Debug.Assert(item is not char i || !char.IsSurrogate(i));
        Debug.Assert(item is byte or char or Rune or U8String);

        switch (item)
        {
            case byte b:
                size = 1;
                return value.IndexOf(b);

            case char c:
                if (char.IsAscii(c))
                {
                    size = 1;
                    return value.IndexOf((byte)c);
                }

                var scalar = U8Scalar.Create(c, checkAscii: false);
                size = scalar.Size;
                return value.IndexOf(scalar.AsSpan());

            case Rune r:
                if (r.IsAscii)
                {
                    size = 1;
                    return value.IndexOf((byte)r.Value);
                }

                var rune = U8Scalar.Create(r, checkAscii: false);
                size = rune.Size;
                return value.IndexOf(rune.AsSpan());

            case U8String str:
                var span = str.AsSpan();
                size = span.Length;
                return IndexOf(value, span);

            default:
                size = 0;
                return ThrowHelpers.Unreachable<int>();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int IndexOf(ReadOnlySpan<byte> value, ReadOnlySpan<byte> item)
    {
        return item.Length is 1 ? value.IndexOf(item.AsRef()) : value.IndexOf(item);
    }
}
