using System.IO.Hashing;

namespace U8Primitives;

public readonly partial struct U8String
{
    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj)
    {
        return obj is U8String other && Equals(other);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(U8String? other)
    {
        return other.HasValue && Equals(other.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(U8String other)
    {
        return AsSpan().SequenceEqual(other.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(byte[]? other)
    {
        return AsSpan().SequenceEqual(other);
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

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    /// <remarks>
    /// The hash code is calculated using the xxHash32 algorithm.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        return (int)XxHash32.HashToUInt32(AsSpan());
    }
}
