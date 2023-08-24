using System.Collections.Immutable;
using System.Globalization;
using System.IO.Hashing;
using System.Runtime.InteropServices;

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
        return U8Comparison.Ordinal.Compare(this, other);
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

    public int CompareTo<T>(U8String other, T comparer)
        where T : IComparer<U8String>
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

    // It seems we really must aggressively inline this. Otherwise, it will always be kept as
    // not inlined, having to pay the full price of comparisons on every call, unlike string.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(U8String other)
    {
        var deref = this;
        if (deref.Length == other.Length)
        {
            if (deref.Offset == other.Offset &&
                ReferenceEquals(deref._value, other._value))
            {
                return true;
            }

            return deref.UnsafeSpan.SequenceEqual(other.UnsafeSpan);
        }

        return false;
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

    public bool Equals<T>(U8String other, T comparer)
        where T : IEqualityComparer<U8String>
    {
        return comparer.Equals(this, other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SourceEquals(U8String other)
    {
        return ReferenceEquals(_value, other._value);
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
}
