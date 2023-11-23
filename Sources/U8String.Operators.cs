using System.Collections.Immutable;
using System.Text;

namespace U8Primitives;

public readonly partial struct U8String
{
    public static U8String operator +(U8String left, byte right) => Concat(left, right);
    public static U8String operator +(U8String left, char right) => Concat(left, right);
    public static U8String operator +(U8String left, Rune right) => Concat(left, right);
    public static U8String operator +(U8String left, U8String right) => Concat(left, right);
    public static U8String operator +(U8String left, byte[] right)
    {
        ThrowHelpers.CheckNull(right);
        return Concat(left, right);
    }
    public static U8String operator +(U8String left, ReadOnlySpan<byte> right) => Concat(left, right);
    public static U8String operator +(byte left, U8String right) => Concat(left, right);
    public static U8String operator +(char left, U8String right) => Concat(left, right);
    public static U8String operator +(Rune left, U8String right) => Concat(left, right);
    public static U8String operator +(byte[] left, U8String right)
    {
        ThrowHelpers.CheckNull(left);
        return Concat(left, right);
    }
    public static U8String operator +(ReadOnlySpan<byte> left, U8String right) => Concat(left, right);

    public static bool operator ==(U8String left, U8String right) => left.Equals(right);
    public static bool operator ==(U8String left, byte[]? right) => left.Equals(right);
    public static bool operator ==(U8String left, ImmutableArray<byte> right) => left.Equals(right);
    public static bool operator ==(U8String left, ReadOnlySpan<byte> right) => left.Equals(right);
    public static bool operator ==(byte[]? left, U8String right) => right.Equals(left);
    public static bool operator ==(ImmutableArray<byte> left, U8String right) => right.Equals(left);
    public static bool operator ==(ReadOnlySpan<byte> left, U8String right) => right.Equals(left);

    public static bool operator !=(U8String left, U8String right) => !(left == right);
    public static bool operator !=(U8String left, byte[]? right) => !(left == right);
    public static bool operator !=(U8String left, ImmutableArray<byte> right) => !(left == right);
    public static bool operator !=(U8String left, ReadOnlySpan<byte> right) => !(left == right);
    public static bool operator !=(byte[]? left, U8String right) => !(left == right);
    public static bool operator !=(ImmutableArray<byte> left, U8String right) => !(left == right);
    public static bool operator !=(ReadOnlySpan<byte> left, U8String right) => !(left == right);

    public static explicit operator U8String(ReadOnlySpan<byte> value) => new(value);
    public static explicit operator U8String(ImmutableArray<byte> value) => new(value);
    public static explicit operator U8String(byte[] value)
    {
        ThrowHelpers.CheckNull(value);
        return new(value);
    }
    public static explicit operator U8String(string value) => new(value);
    public static explicit operator U8String(ReadOnlySpan<char> value) => new(value);

    /// <inheritdoc cref="ToString()"/>
    public static explicit operator string(U8String value) => value.ToString();

    /// <inheritdoc cref="AsSpan()"/>
    public static implicit operator ReadOnlySpan<byte>(U8String value) => value.AsSpan();

    /// <inheritdoc cref="AsMemory()"/>
    public static implicit operator ReadOnlyMemory<byte>(U8String value) => value.AsMemory();
}
