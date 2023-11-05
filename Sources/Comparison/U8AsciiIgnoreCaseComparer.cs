using System.IO.Hashing;
using System.Numerics;
using System.Runtime.Intrinsics;

using U8Primitives.Abstractions;

namespace U8Primitives;

// TODO: Optimize impls.
public readonly struct U8AsciiIgnoreCaseComparer :
    IU8Comparer,
    IU8EqualityComparer,
    IU8ContainsOperator,
    IU8CountOperator,
    IU8IndexOfOperator,
    IU8LastIndexOfOperator,
    IU8StartsWithOperator,
    IU8EndsWithOperator
{
    [ThreadStatic]
    static XxHash3? Hasher;

    public static U8AsciiIgnoreCaseComparer Instance => default;

    public int CommonPrefixLength(U8String left, U8String right)
    {
        if (!left.IsEmpty && !right.IsEmpty)
        {
            var lspan = left.UnsafeSpan;
            var rspan = right.UnsafeSpan;

            return CommonPrefixLength(lspan, rspan);
        }

        return 0;
    }

    public int CommonPrefixLength(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        nuint offset = 0;
        nuint length = (uint)Math.Min(left.Length, right.Length);

        ref var lptr = ref left.AsRef();
        ref var rptr = ref right.AsRef();

        if (length >= (nuint)Vector256<byte>.Count)
        {
            var mask = Vector256.Create((byte)0x20);
            var upperStart = Vector256.Create((byte)'A');
            var upperEnd = Vector256.Create((byte)'Z');

            var lastvec = length - (nuint)Vector256<byte>.Count;
            do
            {
                var lvec = Vector256.LoadUnsafe(ref lptr, offset);
                var rvec = Vector256.LoadUnsafe(ref rptr, offset);

                var lcmask = mask
                    & lvec.Gte(upperStart)
                    & lvec.Lte(upperEnd);

                var rcmask = mask
                    & rvec.Gte(upperStart)
                    & rvec.Lte(upperEnd);

                // Compare normalized left and right vectors and negate the result
                // in order to find the first index of not match.

                var neqmask = ~((lvec | lcmask).Eq(rvec | rcmask));
                if (neqmask != Vector256<byte>.Zero)
                {
                    return neqmask.IndexOfMatch() + (int)offset;
                }
                offset += (nuint)Vector256<byte>.Count;
            } while (offset <= lastvec);
        }

        if (length >= offset + (nuint)Vector128<byte>.Count)
        {
            var lvec = Vector128.LoadUnsafe(ref lptr, offset);
            var rvec = Vector128.LoadUnsafe(ref rptr, offset);

            var lcvec = U8CaseConversion.Ascii.ToLower(lvec);
            var rcvec = U8CaseConversion.Ascii.ToLower(rvec);

            var neqmask = ~lcvec.Eq(rcvec);
            if (neqmask != Vector128<byte>.Zero)
            {
                return neqmask.IndexOfMatch() + (int)offset;
            }
            offset += (nuint)Vector128<byte>.Count;
        }

        if (Vector64.IsHardwareAccelerated &&
            length >= offset + (nuint)Vector64<byte>.Count)
        {
            var lvec = Vector64.LoadUnsafe(ref lptr, offset);
            var rvec = Vector64.LoadUnsafe(ref rptr, offset);

            var lcvec = U8CaseConversion.Ascii.ToLower(lvec);
            var rcvec = U8CaseConversion.Ascii.ToLower(rvec);

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
        if (!x.IsEmpty)
        {
            if (!y.IsEmpty)
            {
                var left = x.UnsafeSpan;
                var right = y.UnsafeSpan;

                return Compare(left, right);
            }

            return 1;
        }

        return y.IsEmpty ? 0 : -1;
    }

    public int Compare(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
    {
        var offset = CommonPrefixLength(x, y);
        var length = Math.Min(x.Length, y.Length);

        ref var xptr = ref x.AsRef();
        ref var yptr = ref y.AsRef();

        while (offset < length)
        {
            var xval = xptr.Add(offset);
            var yval = yptr.Add(offset);

            if (xval != yval)
            {
                xval = U8Info.IsAsciiLetter(xval) ? (byte)(xval & 0b11011111) : xval;
                yval = U8Info.IsAsciiLetter(yval) ? (byte)(yval & 0b11011111) : yval;
                if (xval != yval)
                {
                    return xval - yval;
                }
            }

            offset++;
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
        var count = source.Count(value);

        if (U8Info.IsAsciiLetter(value))
        {
            count += source.Count((byte)(value ^ 0x20));
        }

        return count;
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
        int index;
        var candidate = value[0];
        while (true)
        {
            index = IndexOf(source, candidate).Offset;
            if (index < 0) break;

            source = source.SliceUnsafe(index);
            if (source.Length < value.Length) break;

            if (EqualsCore(
                    ref source.AsRef(),
                    ref value.AsRef(),
                    (uint)value.Length))
            {
                return (index, value.Length);
            }

            source = source.SliceUnsafe(value.Length);
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

    public (int Offset, int Length) LastIndexOf(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
    {
        if (value.Length is 0 || value.Length > source.Length)
        {
            return (-1, 0);
        }

        int index;
        var firstByte = value[0];
        while (true)
        {
            index = LastIndexOf(source, firstByte).Offset;
            if (index < 0) break;

            var candidate = source.SliceUnsafe(index);
            if (candidate.Length < value.Length) break;

            if (EqualsCore(
                    ref candidate.AsRef(),
                    ref value.AsRef(),
                    (nuint)value.Length))
            {
                return (index, value.Length);
            }

            source = source.SliceUnsafe(0, index);
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
                x.Offset != y.Offset || !x.SourceEquals(y)))
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
            var mask = Vector256.Create((byte)0x20);
            var upperStart = Vector256.Create((byte)'A');
            var upperEnd = Vector256.Create((byte)'Z');

            var lastvec = length - (nuint)Vector256<byte>.Count;
            do
            {
                var lvec = Vector256.LoadUnsafe(ref left, offset);
                var rvec = Vector256.LoadUnsafe(ref right, offset);

                var lcmask = mask
                    & lvec.Gte(upperStart)
                    & lvec.Lte(upperEnd);

                var rcmask = mask
                    & rvec.Gte(upperStart)
                    & rvec.Lte(upperEnd);

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

            var lcvec = U8CaseConversion.Ascii.ToLower(lvec);
            var rcvec = U8CaseConversion.Ascii.ToLower(rvec);

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

            var lcvec = U8CaseConversion.Ascii.ToLower(lvec);
            var rcvec = U8CaseConversion.Ascii.ToLower(rvec);

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

    public int GetHashCode(U8String value) => GetHashCode(value.AsSpan());

    public int GetHashCode(ReadOnlySpan<byte> value)
    {
        var buffer = new InlineBuffer128().AsSpan();
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
        var hasher = Interlocked.Exchange(ref Hasher, null);
        hasher ??= new XxHash3(U8Constants.DefaultHashSeed);

        do
        {
            var remainder = Math.Min(length, (uint)buffer.Length);

            U8AsciiCaseConverter.ToUpperCore(
                src: ref src,
                dst: ref buffer.AsRef(),
                remainder);

            hasher.Append(buffer.SliceUnsafe(0, (int)remainder));

            length -= remainder;
        } while (length > 0);

        var hash = hasher.GetCurrentHashAsUInt64();

        hasher.Reset();
        Hasher = hasher;
        return ((int)hash) ^ (int)(hash >> 32);
    }
}
