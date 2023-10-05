using System.Collections.Immutable;
using System.IO.Hashing;
using System.Runtime.InteropServices;
using U8Primitives.Abstractions;

namespace U8Primitives;

public readonly partial struct U8String
{
    public static int Compare(U8String x, U8String y)
    {
        return U8Comparison.Ordinal.Compare(x, y);
    }

    public static int Compare(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
    {
        return U8Comparison.Ordinal.Compare(x, y);
    }

    public static int Compare<T>(U8String x, U8String y, T comparer)
        where T : IComparer<U8String>
    {
        return comparer.Compare(x, y);
    }

    public static int Compare<T>(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y, T comparer)
        where T : IU8Comparer
    {
        return comparer.Compare(x, y);
    }

    /// <summary>
    /// Compares two <see cref="U8String"/> instances using lexicographical semantics and returns
    /// an integer that indicates whether the first instance precedes, follows, or occurs in the same
    /// position in the sort order as the second instance.
    /// </summary>
    public int CompareTo(U8String other)
    {
        return U8Comparison.Ordinal.Compare(this, other);
    }

    public int CompareTo(U8String? other)
    {
        // Supposedly, this is for collections which opt to store 'U8String?'
        return other.HasValue ? CompareTo(other.Value) : 1;
    }

    public int CompareTo(ReadOnlySpan<byte> other)
    {
        return U8Comparison.Ordinal.Compare(this, other);
    }

    public int CompareTo<T>(U8String other, T comparer)
        where T : IComparer<U8String>
    {
        return comparer.Compare(this, other);
    }

    public int CompareTo<T>(ReadOnlySpan<byte> other, T comparer)
        where T : IU8Comparer
    {
        return comparer.Compare(this, other);
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
        if (deref.Length == other.Length)
        {
            if (deref.Length > 0 && (
                deref.Offset != other.Offset || !deref.SourceEquals(other)))
            {
                return deref.UnsafeSpan.SequenceEqual(
                    other.UnsafeSpan.SliceUnsafe(0, deref.Length));
            }

            return true;
        }

        return false;
    }

    public bool Equals(byte[]? other)
    {
        return other != null && Equals(other.AsSpan());
    }

    public bool Equals(ReadOnlySpan<byte> other)
    {
        var deref = this;
        if (deref.Length == other.Length)
        {
            if (deref.Length > 0)
            {
                return deref.UnsafeSpan.SequenceEqual(other);
            }

            return true;
        }

        return false;
    }

    public bool Equals<T>(U8String other, T comparer)
        where T : IEqualityComparer<U8String>
    {
        return comparer.Equals(this, other);
    }

    public bool Equals<T>(ReadOnlySpan<byte> other, T comparer)
        where T : IU8EqualityComparer
    {
        return comparer.Equals(this, other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SourceEquals(U8String other)
    {
        return ReferenceEquals(_value, other._value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SourceEquals(U8Source value)
    {
        return ReferenceEquals(_value, value.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SourceEquals(ImmutableArray<byte> other)
    {
        var arr = ImmutableCollectionsMarshal.AsArray(other);

        return ReferenceEquals(_value, arr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SourceEquals(byte[] other)
    {
        return ReferenceEquals(_value, other);
    }

    /// <inheritdoc cref="GetHashCode(ReadOnlySpan{byte})"/>
    public override int GetHashCode()
    {
        return GetHashCode(AsSpan());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int GetHashCode<T>(T comparer) where T : IEqualityComparer<U8String>
    {
        return comparer.GetHashCode(this);
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <param name="value">UTF-8 bytes to calculate the hash code for.</param>
    /// <remarks>
    /// The hash code is calculated using the xxHash3 algorithm.
    /// </remarks>
    public static int GetHashCode(ReadOnlySpan<byte> value)
    {
        var hash = XxHash3.HashToUInt64(value, U8Constants.DefaultHashSeed);

        return ((int)hash) ^ (int)(hash >> 32);
    }

    public static int GetHashCode<T>(ReadOnlySpan<byte> value, T comparer)
        where T : IU8EqualityComparer
    {
        return comparer.GetHashCode(value);
    }
}
