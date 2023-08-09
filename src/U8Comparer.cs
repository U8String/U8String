using System.Text;

namespace U8Primitives;

public enum U8Comparison
{
    Ordinal = 0,
    OrdinalIgnoreCase = 1,
    AsciiIgnoreCase = 2,
    NormalizationFormC = 3,
    NormalizationFormD = 4,
    NormalizationFormKC = 5,
    NormalizationFormKD = 6,
}

public static class U8Comparer
{
    public static OrdinalComparer Ordinal => default;
    public static OrdinalIgnoreCaseComparer OrdinalIgnoreCase => default;
    public static AsciiIgnoreCaseComparer AsciiIgnoreCase => default;

    public readonly struct OrdinalComparer :
    IComparer<U8String>,
    IComparer<U8String?>,
    IEqualityComparer<U8String>,
    IEqualityComparer<U8String?>
    {
        public static OrdinalComparer Instance => default;

        // TODO: Optimize?
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(U8String x, U8String y)
        {
            return x.Equals(y);
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

        public int GetHashCode(U8String obj)
        {
            return obj.GetHashCode();
        }

        public int GetHashCode(U8String? obj)
        {
            return obj.GetHashCode();
        }
    }

    // TODO: Impl
    public readonly struct OrdinalIgnoreCaseComparer // :
        // IComparer<U8String>,
        // IComparer<U8String?>,
        // IEqualityComparer<U8String>,
        // IEqualityComparer<U8String?>
    {
        public static OrdinalIgnoreCaseComparer Instance => default;
    }

    public readonly struct AsciiIgnoreCaseComparer :
        IComparer<U8String>,
        IComparer<U8String?>,
        IEqualityComparer<U8String>,
        IEqualityComparer<U8String?>
    {
        public static AsciiIgnoreCaseComparer Instance => default;

        public int Compare(U8String x, U8String y) => throw new NotImplementedException();

        public int Compare(U8String? x, U8String? y) => throw new NotImplementedException();

        public bool Equals(U8String x, U8String y)
        {
            var same = ReferenceEquals(x._value, y._value)
                && x.Offset == y.Offset
                && x.Length == y.Length;

            return same || Ascii.EqualsIgnoreCase(x.UnsafeSpan, y.UnsafeSpan);
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

        public int GetHashCode(U8String obj) => throw new NotImplementedException();

        public int GetHashCode(U8String? obj) => throw new NotImplementedException();
    }
}
