using System.Collections.Immutable;
using System.Diagnostics;

using U8.Abstractions;
using U8.Primitives;
using U8.Shared;

namespace U8;

#pragma warning disable RCS1003 // Add braces. Why: manual codegen tuning.
public readonly partial struct U8String
{
    /// <summary>
    /// Compares two <see cref="U8String"/> instances lexicographically.
    /// </summary>
    /// <returns>
    /// A signed integer that indicates the relative position of <paramref name="x"/> and <paramref name="y"/>
    /// in the sort order.
    /// <para/>
    /// Less than zero: <paramref name="x"/> precedes <paramref name="y"/> in the sort order.
    /// <para/>
    /// Zero: <paramref name="x"/> occurs in the same position as <paramref name="y"/> in the sort order.
    /// <para/>
    /// Greater than zero: <paramref name="x"/> follows <paramref name="y"/> in the sort order.
    /// </returns>
    public static int Compare(U8String x, U8String y)
    {
        int result;
        if (!x.IsEmpty)
        {
            if (!y.IsEmpty)
            {
                result = x.UnsafeSpan.SequenceCompareTo(y.UnsafeSpan);
            }
            else result = x.Length;
        }
        else result = -y.Length;

        return result;
    }

    /// <summary>
    /// Compares two byte sequences lexicographically.
    /// </summary>
    /// <returns>
    /// A signed integer that indicates the relative position of <paramref name="x"/> and <paramref name="y"/>
    /// in the sort order.
    /// <para/>
    /// Less than zero: <paramref name="x"/> precedes <paramref name="y"/> in the sort order.
    /// <para/>
    /// Zero: <paramref name="x"/> occurs in the same position as <paramref name="y"/> in the sort order.
    /// <para/>
    /// Greater than zero: <paramref name="x"/> follows <paramref name="y"/> in the sort order.
    /// </returns>
    public static int Compare(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
    {
        return x.SequenceCompareTo(y);
    }

    /// <summary>
    /// Compares two <see cref="U8String"/> instances using specified <paramref name="comparer"/>.
    /// </summary>
    /// <returns>
    /// A signed integer that indicates the relative position of <paramref name="x"/> and <paramref name="y"/>
    /// in the <paramref name="comparer"/>-specific sort order.
    /// </returns>
    public static int Compare<T>(U8String x, U8String y, T comparer)
        where T : IComparer<U8String>
    {
        return comparer.Compare(x, y);
    }

    /// <summary>
    /// Compares two byte sequences using specified <paramref name="comparer"/>.
    /// </summary>
    /// <returns>
    /// A signed integer that indicates the relative position of <paramref name="x"/> and <paramref name="y"/>
    /// in the <paramref name="comparer"/>-specific sort order.
    /// </returns>
    public static int Compare<T>(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y, T comparer)
        where T : IU8Comparer
    {
        return comparer.Compare(x, y);
    }

    /// <summary>
    /// Compares this <see cref="U8String"/> instance with <paramref name="other"/> lexicographically.
    /// </summary>
    /// <returns>
    /// A signed integer that indicates the relative position of this instance and <paramref name="other"/>
    /// in the sort order.
    /// <para/>
    /// Less than zero: This instance precedes <paramref name="other"/> in the sort order.
    /// <para/>
    /// Zero: This instance occurs in the same position as <paramref name="other"/> in the sort order.
    /// <para/>
    /// Greater than zero: This instance follows <paramref name="other"/> in the sort order.
    /// </returns>
    public int CompareTo(U8String other)
    {
        int result;
        var deref = this;
        if (!deref.IsEmpty)
        {
            if (!other.IsEmpty)
            {
                result = deref.UnsafeSpan.SequenceCompareTo(other.UnsafeSpan);
            }
            else result = deref.Length;
        }
        else result = -other.Length;

        return result;
    }

    /// <inheritdoc cref="CompareTo(U8String)"/>
    /// <remarks>
    /// Instances of <paramref name="other"/> that are <see langword="null"/> precede
    /// empty <see cref="U8String"/>s in the sort order.
    /// </remarks>
    public int CompareTo(U8String? other)
    {
        // Supposedly, this is for collections which opt to store 'U8String?'
        if (other.HasValue)
        {
            return CompareTo(other.Value);
        }

        return Length;
    }

    /// <inheritdoc cref="CompareTo(U8String)"/>
    public int CompareTo(ReadOnlySpan<byte> other)
    {
        int result;
        var deref = this;
        if (!deref.IsEmpty)
        {
            if (!other.IsEmpty)
            {
                result = deref.UnsafeSpan.SequenceCompareTo(other);
            }
            else result = deref.Length;
        }
        else result = -other.Length;

        return result;
    }

    /// <summary>
    /// Compares this <see cref="U8String"/> instance with <paramref name="other"/> using specified <paramref name="comparer"/>.
    /// </summary>
    /// <returns>
    /// A signed integer that indicates the relative position of this instance and <paramref name="other"/>
    /// in the <paramref name="comparer"/>-specific sort order.
    /// </returns>
    public int CompareTo<T>(U8String other, T comparer)
        where T : IComparer<U8String>
    {
        return comparer.Compare(this, other);
    }

    /// <inheritdoc cref="CompareTo(U8String)"/>
    public int CompareTo<T>(ReadOnlySpan<byte> other, T comparer)
        where T : IU8Comparer
    {
        return comparer.Compare(this, other);
    }

    /// <summary>
    /// Determines whether this <see cref="U8String"/> instance and a specified object are equal.
    /// </summary>
    /// <remarks>
    /// <paramref name="obj"/> must be either <see cref="U8String"/> or
    /// <see cref="Array"/>/<see cref="ImmutableArray{T}"/> of <see cref="byte"/>s for the comparison to succeed.
    /// </remarks>
    public override bool Equals(object? obj)
    {
        ReadOnlySpan<byte> other;
        if (obj is null)
            goto Unsupported;
        else if (obj is U8String u8str)
        {
            if (!u8str.IsEmpty)
            {
                other = u8str.UnsafeSpan;
            }
            else goto Empty;
        }
        else if (obj.GetType() == typeof(byte[]))
            other = Unsafe.As<byte[]>(obj);
        else if (obj.GetType() == typeof(ImmutableArray<byte>))
            other = Unsafe.Unbox<ImmutableArray<byte>>(obj).AsSpan();
        else goto Unsupported;

        return Equals(other);

    Empty:
        return IsEmpty;

    Unsupported:
        return false;
    }

    /// <summary>
    /// Determines whether this <see cref="U8String"/> instance and <paramref name="other"/> are equal.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if <paramref name="other"/> is not <see langword="null"/> and
    /// is equal byte sequence to this instance; otherwise, <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(U8String? other)
    {
        return other.HasValue && Equals(other.Value);
    }

    /// <summary>
    /// Determines whether this <see cref="U8String"/> instance and <paramref name="other"/> are equal.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if this instance and <paramref name="other"/> are equal byte sequences;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    [DebuggerStepThrough]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(U8String other)
    {
        var deref = this;
        if (deref.Length == other.Length)
        {
            if (deref.Length > 0 && (
                deref.Offset != other.Offset || !deref.SourceEqual(other)))
            {
                return deref.UnsafeSpan.SequenceEqual(other.UnsafeSpan);
            }

            return true;
        }

        return false;
    }

    /// <inheritdoc cref="Equals(U8String?)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(byte[]? other)
    {
        if (other != null)
        {
            return Equals(other.AsSpan());
        }

        return false;
    }

    /// <inheritdoc cref="Equals(U8String)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ImmutableArray<byte> other)
    {
        return Equals(other.AsSpan());
    }

    /// <inheritdoc cref="Equals(U8String)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(ReadOnlySpan<byte> other)
    {
        var deref = this;
        if (deref.Length == other.Length)
        {
            // We cannot replace this with !deref.IsEmpty because .NET won't fold
            // deref.Length > 0 even if other.Length is a greater than zero constant.
            if (deref.Length > 0)
            {
                return deref.UnsafeSpan.SequenceEqual(other);
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether this <see cref="U8String"/> instance and <paramref name="other"/> are equal
    /// using specified <paramref name="comparer"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals<T>(U8String other, T comparer)
        where T : IEqualityComparer<U8String>
    {
        return comparer.Equals(this, other);
    }

    /// <inheritdoc cref="Equals{T}(U8String, T)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals<T>(ReadOnlySpan<byte> other, T comparer)
        where T : IU8EqualityComparer
    {
        return comparer.Equals(this, other);
    }

    /// <summary>
    /// Determines whether this <see cref="U8String"/> instance and <paramref name="other"/>
    /// originate from the same <see cref="U8Source"/>.
    /// </summary>
    /// <remarks>
    /// The most common use case for this method is to check whether two <see cref="U8String"/> instances
    /// are slices of the same <see cref="U8Source"/>, between which the same <see cref="U8Range"/> can be used.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SourceEqual(U8String other)
    {
        return ReferenceEquals(_value, other._value);
    }

    /// <summary>
    /// Determines whether this <see cref="U8String"/> has the same <see cref="U8Source"/> as <paramref name="value"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SourceEqual(U8Source value)
    {
        return ReferenceEquals(_value, value.Value);
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <remarks>
    /// The hash code is calculated using the xxHash3 algorithm.
    /// </remarks>
    public override int GetHashCode()
    {
        var hash = XxHash3.HashToUInt64(this, U8HashSeed.Value);

        return ((int)hash) ^ (int)(hash >> 32);
    }

    /// <summary>
    /// Returns a hash code for this instance calculated with specified <paramref name="comparer"/>.
    /// </summary>
    public int GetHashCode<T>(T comparer) where T : IEqualityComparer<U8String>
    {
        return comparer.GetHashCode(this);
    }

    /// <summary>
    /// Returns a hash code for <paramref name="value"/>.
    /// </summary>
    /// <param name="value">UTF-8 bytes to calculate the hash code for.</param>
    /// <remarks>
    /// The hash code is calculated using the xxHash3 algorithm.
    /// </remarks>
    public static int GetHashCode(ReadOnlySpan<byte> value)
    {
        var hash = XxHash3.HashToUInt64(value, U8HashSeed.Value);

        return ((int)hash) ^ (int)(hash >> 32);
    }

    /// <summary>
    /// Returns a hash code for <paramref name="value"/> calculated with specified <paramref name="comparer"/>.
    /// </summary>
    public static int GetHashCode<T>(ReadOnlySpan<byte> value, T comparer)
        where T : IU8EqualityComparer
    {
        return comparer.GetHashCode(value);
    }
}
