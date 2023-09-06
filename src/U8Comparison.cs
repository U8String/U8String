using System.Buffers;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

using U8Primitives.Abstractions;

namespace U8Primitives;

public static class U8Comparison
{
    // TODO: Make these not nested?
    public static OrdinalComparer Ordinal => default;
    public static AsciiIgnoreCaseComparer AsciiIgnoreCase => default;

    public readonly struct OrdinalComparer :
        IComparer<U8String>,
        IU8EqualityComparer,
        IU8ContainsOperator,
        IU8CountOperator,
        IU8IndexOfOperator
    {
        public static OrdinalComparer Instance => default;

        public int Compare(U8String x, U8String y)
        {
            if (!x.IsEmpty)
            {
                if (!y.IsEmpty)
                {
                    var left = x.UnsafeSpan;
                    var right = y.UnsafeSpan;
                    var result = left.SequenceCompareTo(right);

                    // Clamp between -1 and 1
                    return (result >> 31) | (int)((uint)-result >> 31);
                }

                return 1;
            }

            return y.IsEmpty ? 0 : -1;
        }

        public bool Contains(ReadOnlySpan<byte> source, byte value)
        {
            return source.Contains(value);
        }

        public bool Contains(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
        {
            return source.IndexOf(value) >= 0;
        }

        public int Count(ReadOnlySpan<byte> source, byte value)
        {
            return source.Count(value);
        }

        public int Count(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
        {
            return source.Count(value);
        }

        public bool Equals(U8String x, U8String y)
        {
            return x.Equals(y);
        }

        public bool Equals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
        {
            return left.SequenceEqual(right);
        }

        public int GetHashCode(U8String obj)
        {
            return U8String.GetHashCode(obj);
        }

        public int GetHashCode(ReadOnlySpan<byte> obj)
        {
            return U8String.GetHashCode(obj);
        }

        public (int Offset, int Length) IndexOf(ReadOnlySpan<byte> source, byte value)
        {
            return (source.IndexOf(value), 1);
        }

        public (int Offset, int Length) IndexOf(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
        {
            return (source.IndexOf(value), value.Length);
        }
    }

    // TODO: Optimize impls.
    public readonly struct AsciiIgnoreCaseComparer :
        IComparer<U8String>,
        IU8EqualityComparer,
        IU8ContainsOperator,
        IU8CountOperator,
        IU8IndexOfOperator
    {
        private static readonly SearchValues<byte> AsciiLetters =
            SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"u8);

        public static AsciiIgnoreCaseComparer Instance => default;

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

                if (Unsafe.AreSame(ref lptr, ref rptr))
                {
                    return true;
                }

                return EqualsCore(ref lptr, ref rptr, (nuint)left.Length);
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
                    var lletters = lvec.Gte(upperStart) & lvec.Lte(upperEnd);
                    var rletters = rvec.Gte(upperStart) & rvec.Lte(upperEnd);

                    // Convert both vectors to ASCII lowercase
                    var lcvec = lvec | (lletters & mask);
                    var rcvec = rvec | (rletters & mask);
                    if (lcvec != rcvec)
                    {
                        return false;
                    }
                    offset += (nuint)Vector256<byte>.Count;
                } while (offset <= lastvec);
            }

            if (offset <= length - (nuint)Vector128<byte>.Count)
            {
                var mask = Vector128.Create((byte)0x20);
                var upperStart = Vector128.Create((byte)'A');
                var upperEnd = Vector128.Create((byte)'Z');

                var lvec = Vector128.LoadUnsafe(ref left, offset);
                var rvec = Vector128.LoadUnsafe(ref right, offset);

                var lletters = lvec.Gte(upperStart) & lvec.Lte(upperEnd);
                var rletters = rvec.Gte(upperStart) & rvec.Lte(upperEnd);

                var lcvec = lvec | (lletters & mask);
                var rcvec = rvec | (rletters & mask);
                if (lcvec != rcvec)
                {
                    return false;
                }
                offset += (nuint)Vector128<byte>.Count;
            }

            if (AdvSimd.IsSupported &&
                offset <= length - (nuint)Vector128<byte>.Count)
            {
                var mask = Vector64.Create((byte)0x20);
                var upperStart = Vector64.Create((byte)'A');
                var upperEnd = Vector64.Create((byte)'Z');

                var lvec = Vector64.LoadUnsafe(ref left, offset);
                var rvec = Vector64.LoadUnsafe(ref right, offset);

                var lletters =
                    Vector64.GreaterThanOrEqual(lvec, upperStart) &
                    Vector64.LessThanOrEqual(lvec, upperEnd);
                var rletters =
                    Vector64.GreaterThanOrEqual(rvec, upperStart) &
                    Vector64.LessThanOrEqual(rvec, upperEnd);

                var lcvec = lvec | (lletters & mask);
                var rcvec = rvec | (rletters & mask);
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
                U8Manipulation.ToLowerAscii(
                    src: ref obj.AsRef(),
                    dst: ref buffer.AsRef(),
                    (nuint)obj.Length);

                return U8String.GetHashCode(buffer.SliceUnsafe(0, obj.Length));
            }

            return GetHashCodeCore(ref obj.AsRef(), (nuint)obj.Length, buffer);

            static int GetHashCodeCore(ref byte src, nuint length, ReadOnlySpan<byte> buffer)
            {
                var hashcode = new XxHash3(U8Constants.DefaultHashSeed);
                do
                {
                    var remainder = Math.Min(length, (uint)buffer.Length);

                    U8Manipulation.ToLowerAscii(
                        src: ref src,
                        dst: ref buffer.AsRef(),
                        remainder);

                    hashcode.Append(buffer.SliceUnsafe(0, (int)remainder));

                    length -= remainder;
                } while (length > 0);

                var hash = hashcode.GetCurrentHashAsUInt64();
                return ((int)hash) ^ (int)(hash >> 32);
            }
        }
    }
}
