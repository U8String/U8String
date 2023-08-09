using System.IO.Hashing;

namespace U8Primitives;

public readonly partial struct U8String
{
    /// <summary>
    /// Compares two <see cref="U8String"/> instances using lexicographical semantics and returns
    /// an integer that indicates whether the first instance precedes, follows, or occurs in the same
    /// position in the sort order as the second instance.
    /// </summary>
    public int CompareTo(U8String other)
    {
        return U8Comparer.Ordinal.Compare(this, other);
    }

    public int CompareTo(U8String? other)
    {
        return other.HasValue ? CompareTo(other.Value) : 1;
    }

    public int CompareTo(byte[]? other)
    {
        if (other != null)
        {
            return CompareTo(new U8String(other, 0, other.Length));
        }

        return 1;
    }

    /// <summary>
    /// Indicates whether the current <see cref="U8String"/> instance is equal to another
    /// object of <see cref="U8String"/> or <see cref="byte"/> array.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj switch
        {
            U8String other => Equals(other),
            byte[] other => Equals(other),
            _ => false,
        };
    }

    public bool Equals(U8String? other)
    {
        return other.HasValue && Equals(other.Value);
    }

    public bool Equals(U8String other)
    {
        var deref = this;
        var same = ReferenceEquals(deref._value, other._value)
            && deref.Offset == other.Offset
            && deref.Length == other.Length;

        return same || deref.UnsafeSpan.SequenceEqual(other.UnsafeSpan);
    }

    public bool Equals(byte[]? other)
    {
        return other != null && AsSpan().SequenceEqual(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Span<byte> other)
    {
        return AsSpan().SequenceEqual(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ReadOnlySpan<byte> other)
    {
        return AsSpan().SequenceEqual(other);
    }

    public bool Equals(U8String other, U8Comparison comparisonType)
    {
        return comparisonType switch
        {
            U8Comparison.Ordinal => U8Comparer.Ordinal.Equals(this, other),
            U8Comparison.AsciiIgnoreCase => U8Comparer.AsciiIgnoreCase.Equals(this, other),
            _ => ThrowHelpers.ArgumentOutOfRange<bool>(nameof(comparisonType)),
        };
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <remarks>
    /// The hash code is calculated using the xxHash32 algorithm.
    /// </remarks>
    public override int GetHashCode()
    {
        // TODO: Consider non-default seed?
        var hash = XxHash3.HashToUInt64(this, U8Constants.DefaultHashSeed);

        return ((int)hash) ^ (int)(hash >> 32);
    }
}
