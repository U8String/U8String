namespace U8Primitives;

public readonly partial struct U8String
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String operator +(U8String left, byte right)
    {
        if (!U8Info.IsAsciiByte(right))
        {
            ThrowHelpers.InvalidUtf8();
        }

        return U8Manipulation.ConcatUnchecked(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String operator +(U8String left, U8String right) => Concat(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String operator +(U8String left, byte[] right) => Concat(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String operator +(U8String left, Span<byte> right) => Concat(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String operator +(U8String left, ReadOnlySpan<byte> right) => Concat(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String operator +(byte[] left, U8String right) => Concat(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String operator +(Span<byte> left, U8String right) => Concat(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String operator +(ReadOnlySpan<byte> left, U8String right) => Concat(left, right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(U8String left, U8String right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(U8String left, byte[] right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(U8String left, Span<byte> right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(U8String left, ReadOnlySpan<byte> right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(byte[] left, U8String right) => right.Equals(left);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Span<byte> left, U8String right) => right.Equals(left);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(ReadOnlySpan<byte> left, U8String right) => right.Equals(left);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(U8String left, U8String right) => !(left == right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(U8String left, byte[] right) => !(left == right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(U8String left, Span<byte> right) => !(left == right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(U8String left, ReadOnlySpan<byte> right) => !(left == right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(byte[] left, U8String right) => !(left == right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Span<byte> left, U8String right) => !(left == right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(ReadOnlySpan<byte> left, U8String right) => !(left == right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator U8String(ReadOnlySpan<byte> value)
    {
        return new(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<byte>(U8String value)
    {
        return value.AsSpan();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlyMemory<byte>(U8String value)
    {
        return value.AsMemory();
    }
}
