using System.Diagnostics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Text;

using U8.Abstractions;
using U8.Primitives;

namespace U8.Shared;

// TODO: Better name?
internal static class U8Searching
{
    /// <summary>
    /// Returns the index of the first occurrence of a specified value in a span.
    /// </summary>
    /// <remarks>
    /// Designed to be inlined into the caller and optimized away on constants.
    /// <para>
    /// Contract: when T is char, it must never be a surrogate.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool Contains<T>(ReadOnlySpan<byte> source, T value)
        where T : struct
    {
        Debug.Assert(value is byte or char or Rune or U8String);

        return value switch
        {
            byte b => source.Contains(b),

            char c => char.IsAscii(c)
                ? source.Contains((byte)c)
                : source.IndexOf(new U8Scalar(c, checkAscii: false).AsSpan()) >= 0,

            Rune r => r.IsAscii
                ? source.Contains((byte)r.Value)
                : source.IndexOf(new U8Scalar(r, checkAscii: false).AsSpan()) >= 0,

            U8String str => Contains(source, str.AsSpan()),

            _ => ThrowHelpers.Unreachable<bool>()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool Contains(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
    {
        return value.Length is 1
            ? source.Contains(value[0])
            : source.IndexOf(value) >= 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool Contains<T, C>(ReadOnlySpan<byte> source, T value, C comparer)
        where T : struct
        where C : IU8ContainsOperator
    {
        Debug.Assert(value is not char s || !char.IsSurrogate(s));
        Debug.Assert(value is byte or char or Rune or U8String);

        return value switch
        {
            byte b => comparer.Contains(source, b),

            char c => char.IsAscii(c)
                ? comparer.Contains(source, (byte)c)
                : comparer.Contains(source, new U8Scalar(c, checkAscii: false).AsSpan()),

            Rune r => r.IsAscii
                ? comparer.Contains(source, (byte)r.Value)
                : comparer.Contains(source, new U8Scalar(r, checkAscii: false).AsSpan()),

            U8String str => Contains(source, str.AsSpan(), comparer),

            _ => ThrowHelpers.Unreachable<bool>()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool Contains<T>(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value, T comparer)
        where T : IU8ContainsOperator
    {
        Debug.Assert(!source.IsEmpty);

        return value.Length is 1
            ? comparer.Contains(source, value[0]) // TODO: Verify if this bounds checks
            : comparer.Contains(source, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool ContainsSegment<T>(
        ReadOnlySpan<byte> haystack,
        ReadOnlySpan<byte> needle,
        T separator) where T : unmanaged
    {
        Debug.Assert(separator is byte or char or Rune);
        Debug.Assert(separator is not char s || !char.IsSurrogate(s));

        return separator switch
        {
            byte b => ContainsSegment(haystack, needle, b),

            char c => char.IsAscii(c)
                ? ContainsSegment(haystack, needle, (byte)c)
                : ContainsSegment(haystack, needle, new U8Scalar(c, checkAscii: false).AsSpan()),

            Rune r => r.IsAscii
                ? ContainsSegment(haystack, needle, (byte)r.Value)
                : ContainsSegment(haystack, needle, new U8Scalar(r, checkAscii: false).AsSpan()),

            _ => ThrowHelpers.Unreachable<bool>()
        };
    }

    internal static bool ContainsSegment(
        ReadOnlySpan<byte> haystack,
        ReadOnlySpan<byte> needle,
        byte separator)
    {
        var found = false;
        if (!needle.Contains(separator))
        {
            while (true)
            {
                var matchOffset = haystack.IndexOf(needle);
                // Remaining search space contains no more candidates.
                if (matchOffset < 0)
                {
                    break;
                }
                // Candidate is at the start of the search space.
                else if (matchOffset is 0)
                {
                    // Candidate either equals the search space or is followed by the separator.
                    if (haystack.Length == needle.Length ||
                        haystack.AsRef(needle.Length) == separator)
                    {
                        found = true;
                        break;
                    }
                }
                // Candidate is at the end of the search space.
                else if (matchOffset == haystack.Length - needle.Length)
                {
                    // Candidate is preceded by the separator.
                    if (haystack.AsRef(matchOffset - 1) == separator)
                    {
                        found = true;
                        break;
                    }
                }
                // Candidate is in the middle of the search space.
                else if (
                    haystack.AsRef(matchOffset - 1) == separator &&
                    haystack.AsRef(matchOffset + needle.Length) == separator)
                {
                    found = true;
                    break;
                }

                // Candidate was not at the end of the search space and was
                // not followed by the separator, so we can skip an extra byte.
                haystack = haystack.SliceUnsafe(matchOffset + needle.Length + 1);
            }
        }

        return found;
    }

    internal static bool ContainsSegment(
        ReadOnlySpan<byte> haystack,
        ReadOnlySpan<byte> needle,
        ReadOnlySpan<byte> separator)
    {
        var found = false;
        if (!Contains(needle, separator))
        {
            while (true)
            {
                var matchOffset = haystack.IndexOf(needle);
                // Remaining search space contains no more candidates.
                if (matchOffset < 0)
                {
                    break;
                }
                // Candidate is at the start of the search space.
                else if (matchOffset is 0)
                {
                    // Candidate either equals the search space or is followed by the separator.
                    if (haystack.Length == needle.Length ||
                        haystack.SliceUnsafe(needle.Length)
                                .StartsWith(separator))
                    {
                        found = true;
                        break;
                    }
                }
                // Candidate is at the end of the search space.
                else if (matchOffset == haystack.Length - needle.Length)
                {
                    // Candidate is preceded by the separator.
                    if (haystack.SliceUnsafe(0, matchOffset)
                                .EndsWith(separator))
                    {
                        found = true;
                        break;
                    }
                }
                // Candidate is in the middle of the search space.
                else if (
                    haystack.SliceUnsafe(0, matchOffset)
                            .EndsWith(separator) &&
                    haystack.SliceUnsafe(matchOffset + needle.Length)
                            .StartsWith(separator))
                {
                    found = true;
                    break;
                }

                // Candidate was not at the end of the search space and was
                // not followed by the separator, so we can skip separator.Length too.
                haystack = haystack.SliceUnsafe(
                    matchOffset + needle.Length + separator.Length);
            }
        }

        return found;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool ContainsSegment<T, C>(
        ReadOnlySpan<byte> haystack,
        ReadOnlySpan<byte> needle,
        T separator,
        C comparer)
            where T : struct
            where C : IU8Comparer
    {
        Debug.Assert(separator is byte or char or Rune or U8String);
        Debug.Assert(separator is not char s || !char.IsSurrogate(s));

        return separator switch
        {
            byte b => ContainsSegment(haystack, needle, b, comparer),

            char c => char.IsAscii(c)
                ? ContainsSegment(haystack, needle, (byte)c, comparer)
                : SplitContains(haystack, needle, new U8Scalar(c, checkAscii: false).AsSpan(), comparer),

            Rune r => r.IsAscii
                ? ContainsSegment(haystack, needle, (byte)r.Value, comparer)
                : SplitContains(haystack, needle, new U8Scalar(r, checkAscii: false).AsSpan(), comparer),

            U8String str => SplitContains(haystack, needle, str.AsSpan(), comparer),

            _ => ThrowHelpers.Unreachable<bool>()
        };
    }

    internal static bool ContainsSegment<C>(
        ReadOnlySpan<byte> haystack,
        ReadOnlySpan<byte> needle,
        byte separator,
        C comparer) where C : IU8Comparer
    {
        if (!comparer.Contains(needle, separator))
        {
            while (true)
            {
                var (matchOffset, matchLength) = comparer.IndexOf(haystack, needle);
                // Remaining search space contains no more candidates.
                if (matchOffset < 0)
                {
                    break;
                }
                // Candidate is at the start of the search space.
                else if (matchOffset is 0)
                {
                    // Candidate either equals the search space or is followed by the separator.
                    if (haystack.Length == matchLength ||
                        comparer.StartsWith(haystack.SliceUnsafe(matchLength), separator))
                    {
                        goto Match;
                    }
                }
                // Candidate is at the end of the search space.
                else if (matchOffset == haystack.Length - matchLength)
                {
                    // Candidate is preceded by the separator.
                    if (comparer.EndsWith(haystack.SliceUnsafe(0, matchOffset), separator))
                    {
                        goto Match;
                    }
                }
                // Candidate is in the middle of the search space.
                else if (
                    comparer.EndsWith(haystack.SliceUnsafe(0, matchOffset), separator) &&
                    comparer.StartsWith(haystack.SliceUnsafe(matchOffset + matchLength), separator))
                {
                    goto Match;
                }

                haystack = haystack.SliceUnsafe(matchOffset + matchLength);
            }
        }

        return false;

    // Merging return blocks manually because compiler doesn't want to.
    Match:
        return true;
    }

    internal static bool SplitContains<T>(
        ReadOnlySpan<byte> haystack,
        ReadOnlySpan<byte> needle,
        ReadOnlySpan<byte> separator,
        T comparer) where T : IU8Comparer
    {
        if (!comparer.Contains(needle, separator))
        {
            while (true)
            {
                var (matchOffset, matchLength) = comparer.IndexOf(haystack, needle);
                // Remaining search space contains no more candidates.
                if (matchOffset < 0)
                {
                    break;
                }
                // Candidate is at the start of the search space.
                else if (matchOffset is 0)
                {
                    // Candidate either equals the search space or is followed by the separator.
                    if (haystack.Length == matchLength ||
                        comparer.StartsWith(haystack.SliceUnsafe(matchLength), separator))
                    {
                        goto Match;
                    }
                }
                // Candidate is at the end of the search space.
                else if (matchOffset == haystack.Length - matchLength)
                {
                    // Candidate is preceded by the separator.
                    if (comparer.EndsWith(haystack.SliceUnsafe(0, matchOffset), separator))
                    {
                        goto Match;
                    }
                }
                // Candidate is in the middle of the search space.
                else if (
                    comparer.EndsWith(haystack.SliceUnsafe(0, matchOffset), separator) &&
                    comparer.StartsWith(haystack.SliceUnsafe(matchOffset + matchLength), separator))
                {
                    goto Match;
                }

                haystack = haystack.SliceUnsafe(matchOffset + matchLength);
            }
        }

        return false;

    // Merging return blocks manually because compiler doesn't want to.
    Match:
        return true;
    }

    /// <summary>
    /// Contract: when T is char, it must never be a surrogate.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Count<T>(ReadOnlySpan<byte> source, T value)
        where T : struct
    {
        Debug.Assert(value is not char i || !char.IsSurrogate(i));
        Debug.Assert(value is byte or char or Rune or U8String);

        return value switch
        {
            byte b => source.Count(b),

            char c => char.IsAscii(c)
                ? source.Count((byte)c)
                : source.Count(new U8Scalar(c, checkAscii: false).AsSpan()),

            Rune r => r.IsAscii
                ? source.Count((byte)r.Value)
                : source.Count(new U8Scalar(r, checkAscii: false).AsSpan()),

            U8String str => Count(source, str.AsSpan()),

            _ => ThrowHelpers.Unreachable<int>()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Count(ReadOnlySpan<byte> value, ReadOnlySpan<byte> item)
    {
        // Although span.Count checks internally for Length == 1, the way it is written
        // is not inlineable due to a loop in the default arm of its switch.
        // Therefore, we have to pre-check this here in order to improve the case
        // where item is a single-byte literal, which is expected to be relatively common.
        return item.Length is 1 ? value.Count(item[0]) : value.Count(item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Count<T, C>(ReadOnlySpan<byte> source, T value, C comparer)
        where T : struct
        where C : IU8CountOperator
    {
        Debug.Assert(value is not char i || !char.IsSurrogate(i));
        Debug.Assert(value is byte or char or Rune or U8String);

        return value switch
        {
            byte b => comparer.Count(source, b),

            char c => char.IsAscii(c)
                ? comparer.Count(source, (byte)c)
                : comparer.Count(source, new U8Scalar(c, checkAscii: false).AsSpan()),

            Rune r => r.IsAscii
                ? comparer.Count(source, (byte)r.Value)
                : comparer.Count(source, new U8Scalar(r, checkAscii: false).AsSpan()),

            U8String str => Count(source, str.AsSpan(), comparer),

            _ => ThrowHelpers.Unreachable<int>()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Count<T>(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value, T comparer)
        where T : IU8CountOperator
    {
        return comparer.Count(source, value);
    }

    // Bypass DynamicPGO because it sometimes moves VectorXXX.Create
    // into the loops which is very much not what we want. In this case
    // PGO wins are minor compared to regressions for some of its decisions.
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    internal static nuint CountRunes(ref byte src, nuint length)
    {
        Debug.Assert(length > 0);

        // Adopted from https://github.com/simdutf/simdutf/blob/master/src/generic/utf8.h#L10
        // This method achieves width x2 unrolling by relying on new struct promotion and
        // helpers in VectorExtensions. Operations on 512b and 256b are intentional.
        var count = (nuint)0;
        var offset = (nuint)0;
        ref var ptr = ref Unsafe.As<byte, sbyte>(ref src);

        if (Vector256.IsHardwareAccelerated &&
            length >= (nuint)Vector512<byte>.Count)
        {
            var continuations = Vector512.Create((sbyte)-64);
            var lastvec = length - (nuint)Vector512<byte>.Count;
            do
            {
                var chunk = Vector512.LoadUnsafe(ref ptr, offset);
                var matches = Vector512.LessThan(chunk, continuations);

                count += 64 - matches.AsByte().GetMatchCount();
                offset += (nuint)Vector512<byte>.Count;
            } while (offset <= lastvec);
        }

        // All platforms targeted by .NET 8+ are supposed to support 128b SIMD.
        // If this is not the case, please file an issue (it will work but slowly).
        if (length >= offset + (nuint)Vector256<byte>.Count)
        {
            var continuations = Vector256.Create((sbyte)-64);
            var lastvec = length - (nuint)Vector256<byte>.Count;
            do
            {
                var chunk = Vector256.LoadUnsafe(ref ptr, offset);
                var matches = Vector256.LessThan(chunk, continuations);

                count += 32 - matches.AsByte().GetMatchCount();
                offset += (nuint)Vector256<byte>.Count;

                // Skip this loop if we took the V512 path above
                // since we can only do a single iteration at most.
            } while (!Vector256.IsHardwareAccelerated && offset <= lastvec);
        }

        if (length >= offset + (nuint)Vector128<byte>.Count)
        {
            var continuations = Vector128.Create((sbyte)-64);
            var chunk = Vector128.LoadUnsafe(ref ptr, offset);
            var matches = Vector128.LessThan(chunk, continuations);

            count += 16 - matches.AsByte().GetMatchCount();
            offset += (nuint)Vector128<byte>.Count;
        }

        if (AdvSimd.Arm64.IsSupported &&
            length >= offset + (nuint)Vector64<byte>.Count)
        {
            var continuations = Vector64.Create((sbyte)-64);
            var chunk = Vector64.LoadUnsafe(ref ptr, offset);
            var matches = Vector64.LessThan(chunk, continuations);

            count += 8 - matches.AsByte().GetMatchCount();
            offset += (nuint)Vector64<byte>.Count;
        }

        while (offset < length)
        {
            // Branchless: x86_64: cmp + setge; arm64: cmn + cset
            count += (nuint)(ptr.Add(offset) < -64 ? 0 : 1);
            offset++;
        }

        return count;
    }

    /// <summary>
    /// Contract: when T is char, it must never be a surrogate.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static (int Offset, int Length) IndexOf<T>(ReadOnlySpan<byte> source, T value)
        where T : struct
    {
        Debug.Assert(value is not char i || !char.IsSurrogate(i));
        Debug.Assert(value is byte or char or Rune or U8String);

        switch (value)
        {
            case byte b:
                return (source.IndexOf(b), 1);

            case char c:
                if (char.IsAscii(c))
                {
                    return (source.IndexOf((byte)c), 1);
                }

                var scalar = new U8Scalar(c, checkAscii: false);
                return (source.IndexOf(scalar.AsSpan()), scalar.Length);

            case Rune r:
                if (r.IsAscii)
                {
                    return (source.IndexOf((byte)r.Value), 1);
                }

                var rune = new U8Scalar(r, checkAscii: false);
                return (source.IndexOf(rune.AsSpan()), rune.Length);

            case U8String str:
                var span = str.AsSpan();
                return (IndexOf(source, span), span.Length);

            default:
                return ThrowHelpers.Unreachable<(int, int)>();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int IndexOf(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
    {
        return value.Length is 1 ? source.IndexOf(value[0]) : source.IndexOf(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static (int Offset, int Length) IndexOf<T, C>(ReadOnlySpan<byte> source, T value, C comparer)
        where T : struct
        where C : IU8IndexOfOperator
    {
        Debug.Assert(value is not char i || !char.IsSurrogate(i));
        Debug.Assert(value is byte or char or Rune or U8String);

        switch (value)
        {
            case byte b:
                return comparer.IndexOf(source, b);

            case char c:
                if (char.IsAscii(c))
                {
                    return comparer.IndexOf(source, (byte)c);
                }

                var scalar = new U8Scalar(c, checkAscii: false);
                return comparer.IndexOf(source, scalar.AsSpan());

            case Rune r:
                if (r.IsAscii)
                {
                    return comparer.IndexOf(source, (byte)r.Value);
                }

                var rune = new U8Scalar(r, checkAscii: false);
                return comparer.IndexOf(source, rune.AsSpan());

            case U8String str:
                var span = str.AsSpan();
                return IndexOf(source, span, comparer);

            default:
                return ThrowHelpers.Unreachable<(int, int)>();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static (int Offset, int Length) IndexOf<T>(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value, T comparer)
        where T : IU8IndexOfOperator
    {
        return value.Length is 1
            ? comparer.IndexOf(source, value[0])
            : comparer.IndexOf(source, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static (int Offset, int Length) LastIndexOf<T>(ReadOnlySpan<byte> source, T value)
        where T : struct
    {
        Debug.Assert(value is not char i || !char.IsSurrogate(i));
        Debug.Assert(value is byte or char or Rune or U8String);

        switch (value)
        {
            case byte b:
                return (source.LastIndexOf(b), 1);

            case char c:
                if (char.IsAscii(c))
                {
                    return (source.LastIndexOf((byte)c), 1);
                }

                var scalar = new U8Scalar(c, checkAscii: false);
                return (source.LastIndexOf(scalar.AsSpan()), scalar.Length);

            case Rune r:
                if (r.IsAscii)
                {
                    return (source.LastIndexOf((byte)r.Value), 1);
                }

                var rune = new U8Scalar(r, checkAscii: false);
                return (source.LastIndexOf(rune.AsSpan()), rune.Length);

            case U8String str:
                var span = str.AsSpan();
                return (LastIndexOf(source, span), span.Length);

            default:
                return ThrowHelpers.Unreachable<(int, int)>();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int LastIndexOf(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
    {
        return value.Length is 1 ? source.LastIndexOf(value[0]) : source.LastIndexOf(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static (int Offset, int Length) LastIndexOf<T, C>(ReadOnlySpan<byte> source, T value, C comparer)
        where T : struct
        where C : IU8LastIndexOfOperator
    {
        Debug.Assert(value is not char i || !char.IsSurrogate(i));
        Debug.Assert(value is byte or char or Rune or U8String);

        switch (value)
        {
            case byte b:
                return comparer.LastIndexOf(source, b);

            case char c:
                if (char.IsAscii(c))
                {
                    return comparer.LastIndexOf(source, (byte)c);
                }

                var scalar = new U8Scalar(c, checkAscii: false);
                return comparer.LastIndexOf(source, scalar.AsSpan());

            case Rune r:
                if (r.IsAscii)
                {
                    return comparer.LastIndexOf(source, (byte)r.Value);
                }

                var rune = new U8Scalar(r, checkAscii: false);
                return comparer.LastIndexOf(source, rune.AsSpan());

            case U8String str:
                var span = str.AsSpan();
                return LastIndexOf(source, span, comparer);

            default:
                return ThrowHelpers.Unreachable<(int, int)>();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static (int Offset, int Length) LastIndexOf<T>(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value, T comparer)
        where T : IU8LastIndexOfOperator
    {
        return value.Length is 1
            ? comparer.LastIndexOf(source, value[0])
            : comparer.LastIndexOf(source, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int IndexOfSegment<T>(
        ReadOnlySpan<byte> haystack,
        ReadOnlySpan<byte> needle,
        T separator)
            where T : unmanaged
    {
        Debug.Assert(separator is byte or char or Rune);
        Debug.Assert(separator is not char s || !char.IsSurrogate(s));

        return separator switch
        {
            byte b => IndexOfSegment(haystack, needle, b),

            char c => char.IsAscii(c)
                ? IndexOfSegment(haystack, needle, (byte)c)
                : IndexOfSegment(haystack, needle, new U8Scalar(c, checkAscii: false).AsSpan()),

            Rune r => r.IsAscii
                ? IndexOfSegment(haystack, needle, (byte)r.Value)
                : IndexOfSegment(haystack, needle, new U8Scalar(r, checkAscii: false).AsSpan()),

            _ => ThrowHelpers.Unreachable<int>()
        };
    }

    internal static int IndexOfSegment(
        ReadOnlySpan<byte> haystack,
        ReadOnlySpan<byte> needle,
        byte separator)
    {
        var index = 0;
        if (!needle.Contains(separator))
        {
            while (true)
            {
                var matchOffset = haystack.IndexOf(needle);
                index += matchOffset;

                // Remaining search space contains no more candidates.
                if (matchOffset < 0)
                {
                    index = -1;
                    break;
                }
                // Candidate is at the start of the search space.
                else if (matchOffset is 0)
                {
                    // Candidate either equals the search space or is followed by the separator.
                    if (haystack.Length == needle.Length ||
                        haystack.AsRef(needle.Length) == separator)
                    {
                        break;
                    }
                }
                // Candidate is at the end of the search space.
                else if (matchOffset == haystack.Length - needle.Length)
                {
                    // Candidate is preceded by the separator.
                    if (haystack.AsRef(matchOffset - 1) == separator)
                    {
                        break;
                    }
                }
                // Candidate is in the middle of the search space.
                else if (
                    haystack.AsRef(matchOffset - 1) == separator &&
                    haystack.AsRef(matchOffset + needle.Length) == separator)
                {
                    break;
                }

                // Candidate was not at the end of the search space and was
                // not followed by the separator, so we can skip an extra byte.
                index += needle.Length + 1;
                haystack = haystack.SliceUnsafe(matchOffset + needle.Length + 1);
            }
        }
        else if (needle != [])
        {
            index = -1;
        }

        return index;
    }

    internal static int IndexOfSegment(
        ReadOnlySpan<byte> haystack,
        ReadOnlySpan<byte> needle,
        ReadOnlySpan<byte> separator)
    {
        var index = -1;
        var skipLength = needle.Length + separator.Length;

        if (!Contains(needle, separator))
        {
            while (true)
            {
                var matchOffset = haystack.IndexOf(needle);
                index += matchOffset;

                // Remaining search space contains no more candidates.
                if (matchOffset < 0)
                {
                    index = -1;
                    break;
                }
                // Candidate is at the start of the search space.
                else if (matchOffset is 0)
                {
                    // Candidate either equals the search space or is followed by the separator.
                    if (haystack.Length == needle.Length ||
                        haystack.SliceUnsafe(needle.Length)
                                .StartsWith(separator))
                    {
                        break;
                    }
                }
                // Candidate is at the end of the search space.
                else if (matchOffset == haystack.Length - needle.Length)
                {
                    // Candidate is preceded by the separator.
                    if (haystack.SliceUnsafe(0, matchOffset)
                                .EndsWith(separator))
                    {
                        break;
                    }
                }
                // Candidate is in the middle of the search space.
                else if (
                    haystack.SliceUnsafe(0, matchOffset)
                            .EndsWith(separator) &&
                    haystack.SliceUnsafe(matchOffset + needle.Length)
                            .StartsWith(separator))
                {
                    break;
                }

                // Candidate was not at the end of the search space and was
                // not followed by the separator, so we can skip separator.Length too.
                index += skipLength;
                haystack = haystack.SliceUnsafe(matchOffset + skipLength);
            }
        }

        return index;
    }
}
