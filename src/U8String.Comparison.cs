using System.Runtime.CompilerServices;

namespace U8Primitives;

public readonly partial struct U8String
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(byte value) => AsSpan().Contains(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool StartsWith(U8String value) => AsSpan().StartsWith(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool StartsWith(ReadOnlySpan<byte> value) => AsSpan().StartsWith(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EndsWith(U8String value) => AsSpan().EndsWith(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EndsWith(ReadOnlySpan<byte> value) => AsSpan().EndsWith(value);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        // TODO: Sane implementation
        var hash = new HashCode();
        hash.AddBytes(AsSpan());
        return hash.ToHashCode();
    }
}