using System.Text;

using U8Primitives.Abstractions;

namespace U8Primitives;

public static class U8Comparison
{
    // TODO: Make these not nested?
    public static OrdinalComparer Ordinal => default;
    public static AsciiIgnoreCaseComparer AsciiIgnoreCase => default;

    public readonly struct OrdinalComparer :
        IComparer<U8String>,
        IComparer<U8String?>,
        IEqualityComparer<U8String?>,
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

        public int Compare(U8String? x, U8String? y)
        {
            if (x.HasValue)
            {
                if (y.HasValue)
                {
                    return Compare(x.Value, y.Value);
                }

                return 1;
            }

            return y.HasValue ? -1 : 0;
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

        public bool Equals(U8String x, U8String y) => x.Equals(y);
        public bool Equals(U8String? x, U8String? y)
        {
            if (x.HasValue)
            {
                if (y.HasValue)
                {
                    return Equals(x.Value, y.Value);
                }

                return false;
            }

            return !y.HasValue;
        }

        public bool Equals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right) => left.SequenceEqual(right);

        public int GetHashCode(U8String obj) => U8String.GetHashCode(obj);
        public int GetHashCode(U8String? obj) => obj.GetHashCode();
        public int GetHashCode(ReadOnlySpan<byte> obj) => U8String.GetHashCode(obj);

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
        IComparer<U8String?>,
        IEqualityComparer<U8String?>,
        IU8EqualityComparer,
        IU8ContainsOperator,
        IU8CountOperator,
        IU8IndexOfOperator
    {
        public static AsciiIgnoreCaseComparer Instance => default;

        public int Compare(U8String x, U8String y) => throw new NotImplementedException();

        public int Compare(U8String? x, U8String? y) => throw new NotImplementedException();

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

            throw new NotImplementedException();
        }

        public (int Offset, int Length) IndexOf(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
        {
            // TODO: Impl - mimic what CoreLib SpanHelpers.IndexOf does and apply masked simd search
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

                return Equals(x.UnsafeSpan, y.UnsafeSpan);
            }

            return false;
        }

        public bool Equals(U8String? x, U8String? y)
        {
            if (x.HasValue)
            {
                if (y.HasValue)
                {
                    return Equals(x.Value, y.Value);
                }

                return false;
            }

            return !y.HasValue;
        }

        public bool Equals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
        {
            // TODO: Impl - masked to upper for both vectors in each respective source?
            // An idiom to skip (mask out?) 0x00100000 bit comparison in each vector for ascii letters?
            // Is there a way to do that without having to pay with 4 comparisons (lt+gt for each source vec)?
            // TODO: (for the above) optimized simd elementwise range check?
            throw new NotImplementedException();
        }

        public int GetHashCode(U8String obj) => throw new NotImplementedException();

        public int GetHashCode(U8String? obj) => throw new NotImplementedException();

        public int GetHashCode(ReadOnlySpan<byte> obj)
        {
            // Supposedly, the implementation will set 0x20 bit for all ascii letters
            throw new NotImplementedException();
        }
    }
}
