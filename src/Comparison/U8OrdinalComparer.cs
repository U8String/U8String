using U8Primitives.Abstractions;

namespace U8Primitives;

public readonly struct U8OrdinalComparer :
    IComparer<U8String>,
    IU8EqualityComparer,
    IU8ContainsOperator,
    IU8CountOperator,
    IU8IndexOfOperator,
    IU8LastIndexOfOperator
{
    public static U8OrdinalComparer Instance => default;

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

    public (int Offset, int Length) LastIndexOf(ReadOnlySpan<byte> source, byte value)
    {
        return (source.LastIndexOf(value), 1);
    }

    public (int Offset, int Length) LastIndexOf(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
    {
        return (source.LastIndexOf(value), value.Length);
    }
}