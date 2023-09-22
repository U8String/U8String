using System.Buffers;
using System.IO.Hashing;
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
    IU8LastIndexOfOperator
{
    [ThreadStatic]
    static XxHash3? Hasher;

    public static U8AsciiIgnoreCaseComparer Instance => default;

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
        var offset = x.CommonPrefixLength(y);
        var length = Math.Min(x.Length, y.Length);

        ref var xptr = ref x.AsRef();
        ref var yptr = ref y.AsRef();

        while (offset < length)
        {
            // TODO: Vectorize?
            var xval = xptr.Add(offset);
            var yval = yptr.Add(offset);

            if (xval != yval)
            {
                xval = U8Info.IsAsciiLetter(xval) ? (byte)(xval | 0x20) : xval;
                yval = U8Info.IsAsciiLetter(yval) ? (byte)(yval | 0x20) : yval;
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
        // Maybe it's faster to just do Count(value) + Count(inverted)?
        // TODO: Benchmark alternatives
        if (!U8Info.IsAsciiLetter(value))
        {
            return source.Count(value);
        }

        int index;
        var count = 0;
        while (true)
        {
            index = source.IndexOfAny(value, (byte)(value ^ 0x20));
            if (index < 0) break;

            count++;
            source = source.SliceUnsafe(index + 1);
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
                    (nuint)value.Length))
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

    public bool Equals(U8String x, U8String y)
    {
        if (x.Length == y.Length)
        {
            if (x.Length != 0 && (
                x.Offset != y.Offset || !x.SourceEquals(y)))
            {
                return EqualsCore(
                    ref x.UnsafeRef, ref y.UnsafeRef, (nuint)x.Length);
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
                return EqualsCore(ref lptr, ref rptr, (nuint)x.Length);
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
        var buffer = new InlineBuffer().AsSpan();
        if (value.Length <= buffer.Length)
        {
            U8AsciiCaseConverter.ToUpperCore(
                src: ref value.AsRef(),
                dst: ref buffer.AsRef(),
                (nuint)value.Length);

            return U8String.GetHashCode(buffer.SliceUnsafe(0, value.Length));
        }

        return GetHashCodeLarge(ref value.AsRef(), (nuint)value.Length, buffer);
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
