using System.Diagnostics.CodeAnalysis;

namespace U8Primitives;

public enum U8Comparison
{
    Ordinal = 0,
    OrdinalIgnoreCase = 1,
    NormalizationFormC = 2,
    NormalizationFormD = 3,
    NormalizationFormKC = 4,
    NormalizationFormKD = 5,
}

public static class U8Comparer
{
    public static U8OrdinalComparer Ordinal => default;

    public static U8OrdinalIgnoreCaseComparer OrdinalIgnoreCase => default;
}

public readonly struct U8OrdinalComparer :
    IComparer<U8String>,
    IComparer<U8String?>,
    IEqualityComparer<U8String>,
    IEqualityComparer<U8String?>
{
    public static U8OrdinalComparer Instance => default;

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
public readonly struct U8OrdinalIgnoreCaseComparer // :
    // IComparer<U8String>,
    // IComparer<U8String?>,
    // IEqualityComparer<U8String>,
    // IEqualityComparer<U8String?>
{
    public static U8OrdinalIgnoreCaseComparer Instance => default;
}