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

    public readonly struct AsciiIgnoreCaseComparer :
        IComparer<U8String>,
        IComparer<U8String?>,
        IEqualityComparer<U8String?>,
        IU8EqualityComparer,
        IU8ContainsOperator,
        IU8IndexOfOperator
    {
        public static AsciiIgnoreCaseComparer Instance => default;

        public int Compare(U8String x, U8String y) => throw new NotImplementedException();

        public int Compare(U8String? x, U8String? y) => throw new NotImplementedException();

        public bool Contains(ReadOnlySpan<byte> source, byte value)
        {
            throw new NotImplementedException();
        }

        public bool Contains(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
        {
            throw new NotImplementedException();
        }

        public (int Offset, int Length) IndexOf(ReadOnlySpan<byte> source, byte value)
        {
            throw new NotImplementedException();
        }

        public (int Offset, int Length) IndexOf(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
        {
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

                // Change to custom implementation which performs ordinal comparison of non-ascii bytes
                return Ascii.EqualsIgnoreCase(x.UnsafeSpan, y.UnsafeSpan);
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
            throw new NotImplementedException();
        }

        public int GetHashCode(U8String obj) => throw new NotImplementedException();

        public int GetHashCode(U8String? obj) => throw new NotImplementedException();

        public int GetHashCode(ReadOnlySpan<byte> obj)
        {
            throw new NotImplementedException();
        }
    }
}
