using U8Primitives.Abstractions;

namespace U8Primitives;

public readonly struct U8OrdinalComparer :
    IU8Comparer,
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

                return Compare(left, right);
            }

            return 1;
        }

        return y.IsEmpty ? 0 : -1;
    }

    public int Compare(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
    {
        // TODO: Does this need to be clamped?
        return x.SequenceCompareTo(y);
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

    public bool Equals(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
    {
        return x.SequenceEqual(y);
    }

    public int GetHashCode(U8String value)
    {
        return U8String.GetHashCode(value);
    }

    public int GetHashCode(ReadOnlySpan<byte> value)
    {
        return U8String.GetHashCode(value);
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