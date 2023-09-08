using System.Buffers;
using System.IO.Hashing;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

using U8Primitives.Abstractions;

namespace U8Primitives;

// TODO: Optimize impls.
public readonly struct U8AsciiIgnoreCaseComparer :
    IComparer<U8String>,
    IU8EqualityComparer,
    IU8ContainsOperator,
    IU8CountOperator,
    IU8IndexOfOperator
{
    static readonly SearchValues<byte> AsciiLetters =
        SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"u8);

    [ThreadStatic]
    static XxHash3? Hasher;

    public static U8AsciiIgnoreCaseComparer Instance => default;

    public int Compare(U8String x, U8String y)
    {
        throw new NotImplementedException();
    }

    public bool Contains(ReadOnlySpan<byte> source, byte value)
    {
        return U8Info.IsAsciiLetter(value)
            ? source.ContainsAny(value, (byte)(value ^ 0x20))
            : source.Contains(value);
    }

    public bool Contains(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
    {
        if (value.Length is 1)
        {
            return Contains(source, value[0]);
        }

        // TODO: options
        // - Check if input has ascii letters, then use default search or custom masked simd search
        // - Use custom masked simd search from the get go
        // - Probably call into IndexOf like in other locations
        throw new NotImplementedException();
    }

    public int Count(ReadOnlySpan<byte> source, byte value)
    {
        throw new NotImplementedException();
    }

    public int Count(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
    {
        throw new NotImplementedException();
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
        // Strategy:
        // - Iterate through candidates which are determined by the first needle byte match
        // or the first needle vector match (to avoid examining candidates byte-by-byte)
        // - For each candidate, slice the source to the same length as the needle
        // if remainder.Length > value.Length and then delegate comparison to
        // EqualsAsciiIgnoreCase implementation
        // TODO: specialize sub-needle lengths to perform masked search instead?
        var needleHead = value[..value.IndexOfAny(AsciiLetters)];
        var needleAscii = value[needleHead.Length..];

        throw new NotImplementedException();
    }

    public bool Equals(U8String x, U8String y)
    {
        if (x.Length == y.Length)
        {
            if (x.Offset == y.Offset && x.SourceEquals(y))
            {
                return true;
            }

            return EqualsCore(
                ref x.UnsafeRef,
                ref y.UnsafeRef,
                (nuint)x.Length);
        }

        return false;
    }

    public bool Equals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        if (left.Length == right.Length)
        {
            ref var lptr = ref left.AsRef();
            ref var rptr = ref right.AsRef();

            if (!Unsafe.AreSame(ref lptr, ref rptr))
            {
                return EqualsCore(ref lptr, ref rptr, (nuint)left.Length);
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

                // Create ASCII uppercase letters eqmasks
                // TODO: Do we really need to do 2(CMHSx2+AND) here?
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

    public int GetHashCode(U8String obj) => GetHashCode(obj.AsSpan());

    public int GetHashCode(ReadOnlySpan<byte> obj)
    {
        var buffer = new InlineBuffer().AsSpan();
        if (obj.Length <= buffer.Length)
        {
            U8AsciiCaseConverter.ToLowerCore(
                src: ref obj.AsRef(),
                dst: ref buffer.AsRef(),
                (nuint)obj.Length);

            return U8String.GetHashCode(buffer.SliceUnsafe(0, obj.Length));
        }

        return GetHashCodeLarge(ref obj.AsRef(), (nuint)obj.Length, buffer);
    }

    static int GetHashCodeLarge(ref byte src, nuint length, ReadOnlySpan<byte> buffer)
    {
        var hasher = Interlocked.Exchange(ref Hasher, null);
        hasher ??= new XxHash3(U8Constants.DefaultHashSeed);

        do
        {
            var remainder = Math.Min(length, (uint)buffer.Length);

            U8AsciiCaseConverter.ToLowerCore(
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
