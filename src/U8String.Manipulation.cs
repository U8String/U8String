using System.Runtime.CompilerServices;

namespace U8Primitives;

public readonly partial struct U8String
{
    // TODO: Optimize/deduplicate Concat variants
    // TODO: Investigate if it is possible fold validation for u8 literals
    public static U8String Concat(U8String left, U8String right)
    {
        if (left.IsEmpty)
        {
            return right;
        }

        if (right.IsEmpty)
        {
            return left;
        }

        var length = left._length + right._length;
        var value = new byte[length];

        left.AsSpan().CopyTo(value);
        right.AsSpan().CopyTo(value.AsSpan((int)left._length));

        return new U8String(value, 0, length);
    }

    public static U8String Concat(U8String left, ReadOnlySpan<byte> right)
    {
        if (right.IsEmpty)
        {
            return left;
        }

        Validate(right);
        if (left.IsEmpty)
        {
            return new U8String(right, skipValidation: true);
        }

        var length = (uint)(left.Length + right.Length);
        var value = new byte[length];

        left.AsSpan().CopyTo(value);
        right.CopyTo(value.AsSpan(left.Length));

        return new U8String(value, 0, length);
    }

    public static U8String Concat(ReadOnlySpan<byte> left, U8String right)
    {
        if (left.IsEmpty)
        {
            return right;
        }

        Validate(left);
        if (right.IsEmpty)
        {
            return new U8String(left, skipValidation: true);
        }

        var length = (uint)(left.Length + right.Length);
        var value = new byte[length];

        left.CopyTo(value);
        right.AsSpan().CopyTo(value.AsSpan(left.Length));

        return new U8String(value, 0, length);
    }

    public static U8String Concat(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        Validate(left);
        if (right.IsEmpty)
        {
            return new U8String(left, skipValidation: true);
        }

        Validate(right);
        if (left.IsEmpty)
        {
            return new U8String(right);
        }

        var length = (uint)(left.Length + right.Length);
        var value = new byte[length];

        left.CopyTo(value);
        right.CopyTo(value.AsSpan(left.Length));

        return new U8String(value, 0, length);
    }

    public U8String Replace(byte oldValue, byte newValue)
    {
        if (IsEmpty)
        {
            return this;
        }

        var current = AsSpan();
        var firstReplace = current.IndexOf(oldValue);
        if (firstReplace < 0)
        {
            return this;
        }

        var replaced = new byte[_length];
        current[firstReplace..].Replace(
            replaced.AsSpan(firstReplace..),
            oldValue,
            newValue);

        return new U8String(replaced, 0, _length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String Trim()
    {
        if (IsEmpty || (
            !IndexUnsafe((uint)0).IsWhitespace() &&
            !IndexUnsafe(_length - 1).IsWhitespace()))
        {
            return this;
        }

        return TrimCore();
    }

    private U8String TrimCore()
    {
        var span = AsSpan();
        var start = 0;
        while (start < span.Length && span[start].IsWhitespace())
        {
            start++;
        }

        var end = (int)(_length - 1);
        while (end >= start && span[end].IsWhitespace())
        {
            end--;
        }

        return end - start > 0
            ? this[start..++end]
            : default;
    }
}
