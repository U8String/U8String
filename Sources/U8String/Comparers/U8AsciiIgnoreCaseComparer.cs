using System.Numerics;
using System.Runtime.Intrinsics;

using U8.Abstractions;
using U8.CaseConversion;
using U8.Shared;

namespace U8.Comparison;

// TODO: Optimize impls.
// TODO: Decide where in the sort order should the case-folded characters go.
#pragma warning disable RCS1003, IDE0045 // Add braces and simplify branching. Why: manual block ordering.
public readonly struct U8AsciiIgnoreCaseComparer : IU8Comparer
{
    [ThreadStatic]
    static XxHash3? Hasher;

    public static U8AsciiIgnoreCaseComparer Instance => default;

    public static int CommonPrefixLength(U8String left, U8String right)
    {
        if (!left.IsEmpty && !right.IsEmpty)
        {
            var lspan = left.UnsafeSpan;
            var rspan = right.UnsafeSpan;

            return CommonPrefixLength(lspan, rspan);
        }

        return 0;
    }

    public static int CommonPrefixLength(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        nuint offset = 0;
        nuint length = (uint)Math.Min(left.Length, right.Length);

        ref var lptr = ref left.AsRef();
        ref var rptr = ref right.AsRef();

        if (length >= (nuint)Vector256<byte>.Count)
        {
            var mask = Vector256.Create((sbyte)0x20);
            var overflow = Vector256.Create<sbyte>(128 - 'A');
            var bound = Vector256.Create<sbyte>(-127 + ('Z' - 'A'));

            var lastvec = length - (nuint)Vector256<byte>.Count;
            do
            {
                var lvec = Vector256.LoadUnsafe(ref lptr, offset).AsSByte();
                var rvec = Vector256.LoadUnsafe(ref rptr, offset).AsSByte();

                var lcmask = (lvec + overflow).Lt(bound) & mask;
                var rcmask = (rvec + overflow).Lt(bound) & mask;

                // Compare normalized left and right vectors and negate the result
                // in order to find the first index of not match.

                var neqmask = ~((lvec | lcmask).Eq(rvec | rcmask));
                if (neqmask != Vector256<sbyte>.Zero)
                {
                    return (int)(uint)(neqmask.IndexOfMatch() + offset);
                }
                offset += (nuint)Vector256<byte>.Count;
            } while (offset <= lastvec);
        }

        if (length >= offset + (nuint)Vector128<byte>.Count)
        {
            var lvec = Vector128.LoadUnsafe(ref lptr, offset);
            var rvec = Vector128.LoadUnsafe(ref rptr, offset);

            var lcvec = U8AsciiCaseConverter.ToLower(lvec);
            var rcvec = U8AsciiCaseConverter.ToLower(rvec);

            var neqmask = ~lcvec.Eq(rcvec);
            if (neqmask != Vector128<byte>.Zero)
            {
                return (int)(uint)(neqmask.IndexOfMatch() + offset);
            }
            offset += (nuint)Vector128<byte>.Count;
        }

        if (Vector64.IsHardwareAccelerated &&
            length >= offset + (nuint)Vector64<byte>.Count)
        {
            var lvec = Vector64.LoadUnsafe(ref lptr, offset);
            var rvec = Vector64.LoadUnsafe(ref rptr, offset);

            var lcvec = U8AsciiCaseConverter.ToLower(lvec);
            var rcvec = U8AsciiCaseConverter.ToLower(rvec);

            var neqmask = ~Vector64.Equals(lcvec, rcvec);
            if (neqmask != Vector64<byte>.Zero)
            {
                return BitOperations.TrailingZeroCount(
                    neqmask.AsUInt64().ToScalar()) + (int)offset;
            }
            offset += (nuint)Vector64<byte>.Count;
        }

        while (offset < length)
        {
            var l = lptr.Add(offset);
            var r = rptr.Add(offset);

            if (l != r && (!U8Info.IsAsciiLetter(l) || (l ^ 0x20) != r))
            {
                return (int)offset;
            }

            offset++;
        }

        return (int)length;
    }

    public int Compare(U8String x, U8String y)
    {
        int result;
        if (!x.IsEmpty)
        {
            if (!y.IsEmpty)
            {
                result = Compare(x.UnsafeSpan, y.UnsafeSpan);
            }
            else result = x.Length;
        }
        else result = -y.Length;

        return result;
    }

    public int Compare(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
    {
        var offset = CommonPrefixLength(x, y);
        var length = Math.Min(x.Length, y.Length);

        if (offset < length)
        {
            var xval = x.AsRef(offset);
            var yval = y.AsRef(offset);

            xval = U8Info.IsAsciiLetter(xval) ? (byte)(xval & 0b11011111) : xval;
            yval = U8Info.IsAsciiLetter(yval) ? (byte)(yval & 0b11011111) : yval;

            return xval - yval;
        }

        return x.Length - y.Length;
    }

    public bool Contains(ReadOnlySpan<byte> source, byte value)
    {
        if (!U8Info.IsAsciiLetter(value))
        {
            return source.Contains(value);
        }

        return source.ContainsAny(value, (byte)(value ^ 0x20));
    }

    public bool Contains(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
    {
        return IndexOf(source, value).Offset >= 0;
    }

    public int Count(ReadOnlySpan<byte> source, byte value)
    {
        var count = U8Searching.CountByte(value, ref source.AsRef(), (uint)source.Length);

        // TODO: Impl. CountTwoBytes
        if (U8Info.IsAsciiLetter(value))
        {
            count += U8Searching.CountByte((byte)(value ^ 0x20), ref source.AsRef(), (uint)source.Length);
        }

        return (int)(uint)count;
    }

    public int Count(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
    {
        if (value.Length is 0)
        {
            return 0;
        }

        if (value.Length is 1)
        {
            return Count(source, value[0]);
        }

        int index;
        var count = 0;
        while (true)
        {
            index = IndexOf(source, value).Offset;
            if (index < 0) break;

            count++;
            source = source.SliceUnsafe(index + value.Length);
        }

        return count;
    }

    public (int Offset, int Length) IndexOf(ReadOnlySpan<byte> source, byte value)
    {
        if (!U8Info.IsAsciiLetter(value))
        {
            return (source.IndexOf(value), 1);
        }

        return (source.IndexOfAny(value, (byte)(value ^ 0x20)), 1);
    }

    public (int Offset, int Length) IndexOf(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
    {
        if (value.Length is 0 || value.Length > source.Length)
        {
            return (-1, 0);
        }

        // Performance of this implementation jumps off a cliff on sequences of repeated
        // needle candidates, e.g. "aaaaa". The comparer itself is probably a stop-gap measure
        // until there is proper U8OrdinalIgnoreCaseComparer implementation.
        var index = 0;
        var candidate = value[0];
        while (true)
        {
            var matchOffset = IndexOf(source, candidate).Offset;
            if (matchOffset < 0) break;

            index += matchOffset;
            source = source.SliceUnsafe(matchOffset);
            if (source.Length < value.Length) break;

            var commonPrefix = CommonPrefixLength(source, value);
            if (commonPrefix == value.Length)
            {
                return (index, value.Length);
            }

            index += commonPrefix;
            source = source.SliceUnsafe(commonPrefix);
        }

        return (-1, 0);
    }

    public (int Offset, int Length) LastIndexOf(ReadOnlySpan<byte> source, byte value)
    {
        if (!U8Info.IsAsciiLetter(value))
        {
            return (source.LastIndexOf(value), 1);
        }

        return (source.LastIndexOfAny(value, (byte)(value ^ 0x20)), 1);
    }

    // TODO: !FIX ME!
    public (int Offset, int Length) LastIndexOf(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
    {
        if (value.Length is 0 || value.Length > source.Length)
        {
            return (-1, 0);
        }

        var firstByte = value[0];
        while (true)
        {
            var matchOffset = LastIndexOf(source, firstByte).Offset;
            if (matchOffset < 0) break;

            var candidate = source.SliceUnsafe(matchOffset);
            if (candidate.Length < value.Length) break;

            if (EqualsCore(
                    ref candidate.AsRef(),
                    ref value.AsRef(),
                    (nuint)value.Length))
            {
                return (matchOffset, value.Length);
            }

            source = source.SliceUnsafe(0, matchOffset);
        }

        return (-1, 0);
    }

    public bool StartsWith(ReadOnlySpan<byte> source, byte value)
    {
        if (source.Length > 0)
        {
            var b0 = source[0];

            return b0 == value || (U8Info.IsAsciiLetter(b0) && (b0 ^ 0x20) == value);
        }

        return false;
    }

    public bool StartsWith(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
    {
        if (source.Length >= value.Length)
        {
            if (source.Length > 0)
            {
                return EqualsCore(
                    ref source.AsRef(),
                    ref value.AsRef(),
                    (uint)value.Length);
            }

            return true;
        }

        return false;
    }

    public bool EndsWith(ReadOnlySpan<byte> source, byte value)
    {
        if (source.Length > 0)
        {
            var b0 = source[^1];

            return b0 == value || (U8Info.IsAsciiLetter(b0) && (b0 ^ 0x20) == value);
        }

        return false;
    }

    public bool EndsWith(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
    {
        if (source.Length >= value.Length)
        {
            if (source.Length > 0)
            {
                return EqualsCore(
                    ref source.AsRef(source.Length - value.Length),
                    ref value.AsRef(),
                    (uint)value.Length);
            }

            return true;
        }

        return false;
    }

    public bool Equals(U8String x, U8String y)
    {
        if (x.Length == y.Length)
        {
            if (x.Length != 0 && (
                x.Offset != y.Offset || !x.SourceEqual(y)))
            {
                return EqualsCore(
                    ref x.UnsafeRef, ref y.UnsafeRef, (uint)x.Length);
            }

            return true;
        }

        return false;
    }

    public bool Equals(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
    {
        if (x.Length == y.Length)
        {
            ref var lptr = ref x.AsRef();
            ref var rptr = ref y.AsRef();

            if (!Unsafe.AreSame(ref lptr, ref rptr))
            {
                return EqualsCore(ref lptr, ref rptr, (uint)x.Length);
            }

            return true;
        }

        return false;
    }

    static bool EqualsCore(ref byte left, ref byte right, nuint length)
    {
        nuint offset = 0;

        if (length >= (nuint)Vector256<byte>.Count)
        {
            var mask = Vector256.Create((sbyte)0x20);
            var overflow = Vector256.Create<sbyte>(128 - 'A');
            var bound = Vector256.Create<sbyte>(-127 + ('Z' - 'A'));

            var lastvec = length - (nuint)Vector256<byte>.Count;
            do
            {
                var lvec = Vector256.LoadUnsafe(ref left, offset).AsSByte();
                var rvec = Vector256.LoadUnsafe(ref right, offset).AsSByte();

                var lcmask = (lvec + overflow).Lt(bound) & mask;
                var rcmask = (rvec + overflow).Lt(bound) & mask;

                // Compare left and right with ASCII letters normalized to lowercase
                if ((lvec | lcmask) != (rvec | rcmask))
                {
                    return false;
                }
                offset += (nuint)Vector256<byte>.Count;
            } while (offset <= lastvec);
        }

        if (length >= offset + (nuint)Vector128<byte>.Count)
        {
            var lvec = Vector128.LoadUnsafe(ref left, offset);
            var rvec = Vector128.LoadUnsafe(ref right, offset);

            var lcvec = U8AsciiCaseConverter.ToLower(lvec);
            var rcvec = U8AsciiCaseConverter.ToLower(rvec);

            if (lcvec != rcvec)
            {
                return false;
            }
            offset += (nuint)Vector128<byte>.Count;
        }

        if (Vector64.IsHardwareAccelerated &&
            length >= offset + (nuint)Vector64<byte>.Count)
        {
            var lvec = Vector64.LoadUnsafe(ref left, offset);
            var rvec = Vector64.LoadUnsafe(ref right, offset);

            var lcvec = U8AsciiCaseConverter.ToLower(lvec);
            var rcvec = U8AsciiCaseConverter.ToLower(rvec);

            if (lcvec != rcvec)
            {
                return false;
            }
            offset += (nuint)Vector64<byte>.Count;
        }

        while (offset < length)
        {
            var l = left.Add(offset);
            var r = right.Add(offset);

            if (l != r && (!U8Info.IsAsciiLetter(l) || (l ^ 0x20) != r))
            {
                return false;
            }

            offset++;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetHashCode(U8String value) => GetHashCode(value.AsSpan());

    [SkipLocalsInit]
    public int GetHashCode(ReadOnlySpan<byte> value)
    {
        var buffer = (stackalloc byte[256]);
        if (value.Length <= buffer.Length)
        {
            U8AsciiCaseConverter.ToUpperCore(
                src: ref value.AsRef(),
                dst: ref buffer.AsRef(),
                (uint)value.Length);

            return U8String.GetHashCode(buffer.SliceUnsafe(0, value.Length));
        }

        return GetHashCodeLarge(ref value.AsRef(), (uint)value.Length, buffer);
    }

    static int GetHashCodeLarge(ref byte src, nuint length, ReadOnlySpan<byte> buffer)
    {
        var hasher = Hasher ??= new XxHash3(U8HashSeed.Value);

        do
        {
            var remainder = Math.Min(length, (uint)buffer.Length);

            U8AsciiCaseConverter.ToUpperCore(
                src: ref src,
                dst: ref buffer.AsRef(),
                remainder);

            hasher.Append(buffer.SliceUnsafe(0, (int)remainder));

            src = ref src.Add(remainder);
            length -= remainder;
        } while (length > 0);

        var hash = hasher.GetCurrentHashAsUInt64();

        hasher.Reset();
        return ((int)hash) ^ (int)(hash >> 32);
    }
}
