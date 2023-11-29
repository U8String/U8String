using System.Text;
using U8Primitives.Abstractions;

namespace U8Primitives;

#pragma warning disable RCS1003, RCS1179, IDE0045 // Braces, ternary, etc. Why: explicit control over block layout and return merging.
public readonly partial struct U8String
{
    public int CommonPrefixLength(U8String other)
    {
        if (!other.IsEmpty)
        {
            return CommonPrefixLength(other.UnsafeSpan);
        }

        return 0;
    }

    public int CommonPrefixLength(ReadOnlySpan<byte> other)
    {
        var deref = this;
        if (!deref.IsEmpty)
        {
            return deref.UnsafeSpan.CommonPrefixLength(other);
        }

        return 0;
    }

    public bool Contains(byte value)
    {
        bool result;
        var deref = this;
        if (!deref.IsEmpty)
        {
            result = deref.UnsafeSpan.Contains(value);
        }
        else result = false;

        return result;
    }

    public bool Contains(char value)
    {
        ThrowHelpers.CheckSurrogate(value);

        return char.IsAscii(value)
            ? Contains((byte)value)
            : Contains(new U8Scalar(value, checkAscii: false).AsSpan());
    }

    public bool Contains(Rune value)
    {
        return value.IsAscii
            ? Contains((byte)value.Value)
            : Contains(new U8Scalar(value, checkAscii: false).AsSpan());
    }

    public bool Contains(U8String value)
    {
        bool result;
        var deref = this;
        if (!value.IsEmpty)
        {
            if (!deref.IsEmpty)
            {
                result = deref.UnsafeSpan.IndexOf(value.UnsafeSpan) >= 0;
            }
            else result = false;
        }
        else result = true;

        return result;
    }

    public bool Contains(ReadOnlySpan<byte> value)
    {
        bool result;
        var deref = this;
        if (!deref.IsEmpty)
        {
            result = deref.UnsafeSpan.IndexOf(value) >= 0;
        }
        else result = value.IsEmpty;

        return result;
    }

    public bool Contains<T>(byte value, T comparer)
        where T : IU8ContainsOperator
    {
        return comparer.Contains(this, value);
    }

    public bool Contains<T>(char value, T comparer)
        where T : IU8ContainsOperator
    {
        ThrowHelpers.CheckSurrogate(value);

        return char.IsAscii(value)
            ? Contains((byte)value, comparer)
            : Contains(new U8Scalar(value, checkAscii: false).AsSpan(), comparer);
    }

    public bool Contains<T>(Rune value, T comparer)
        where T : IU8ContainsOperator
    {
        return value.IsAscii
            ? Contains((byte)value.Value, comparer)
            : Contains(new U8Scalar(value, checkAscii: false).AsSpan(), comparer);
    }

    public bool Contains<T>(U8String value, T comparer)
        where T : IU8ContainsOperator
    {
        return comparer.Contains(this, value);
    }

    public bool Contains<T>(ReadOnlySpan<byte> value, T comparer)
        where T : IU8ContainsOperator
    {
        return comparer.Contains(this, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool StartsWith(byte value)
    {
        var (arr, offset) = (_value, Offset);
        return arr != null && arr.AsRef(offset) == value;
    }

    public bool StartsWith(char value)
    {
        ThrowHelpers.CheckSurrogate(value);

        return char.IsAscii(value)
            ? StartsWith((byte)value)
            : StartsWith(new U8Scalar(value, checkAscii: false).AsSpan());
    }

    public bool StartsWith(Rune value) => value.IsAscii
        ? StartsWith((byte)value.Value)
        : StartsWith(new U8Scalar(value, checkAscii: false).AsSpan());

    public bool StartsWith(U8String value)
    {
        bool result;
        var deref = this;
        if (!value.IsEmpty)
        {
            if (!deref.IsEmpty && value.Length <= deref.Length)
            {
                result = deref.UnsafeSpan.StartsWith(value.UnsafeSpan);
            }
            else result = false;
        }
        else result = true;

        return result;
    }

    public bool StartsWith(ReadOnlySpan<byte> value)
    {
        bool result;
        var deref = this;
        if (!deref.IsEmpty && value.Length <= deref.Length)
        {
            result = deref.UnsafeSpan.StartsWith(value);
        }
        else result = value.IsEmpty;

        return result;
    }

    public bool StartsWith<T>(byte value, T comparer)
        where T : IU8StartsWithOperator
    {
        return comparer.StartsWith(this, value);
    }

    public bool StartsWith<T>(char value, T comparer)
        where T : IU8StartsWithOperator
    {
        ThrowHelpers.CheckSurrogate(value);

        return char.IsAscii(value)
            ? StartsWith((byte)value, comparer)
            : StartsWith(new U8Scalar(value, checkAscii: false).AsSpan(), comparer);
    }

    public bool StartsWith<T>(Rune value, T comparer)
        where T : IU8StartsWithOperator
    {
        return value.IsAscii
            ? StartsWith((byte)value.Value, comparer)
            : StartsWith(new U8Scalar(value, checkAscii: false).AsSpan(), comparer);
    }

    public bool StartsWith<T>(U8String value, T comparer)
        where T : IU8StartsWithOperator
    {
        return comparer.StartsWith(this, value);
    }

    public bool StartsWith<T>(ReadOnlySpan<byte> value, T comparer)
        where T : IU8StartsWithOperator
    {
        return comparer.StartsWith(this, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EndsWith(byte value)
    {
        var (arr, offset, length) = this;
        return arr != null && arr.AsRef(offset + length - 1) == value;
    }

    public bool EndsWith(char value)
    {
        ThrowHelpers.CheckSurrogate(value);

        return char.IsAscii(value)
            ? EndsWith((byte)value)
            : EndsWith(new U8Scalar(value, checkAscii: false).AsSpan());
    }

    public bool EndsWith(Rune value) => value.IsAscii
        ? EndsWith((byte)value.Value)
        : EndsWith(new U8Scalar(value, checkAscii: false).AsSpan());

    public bool EndsWith(U8String value)
    {
        bool result;
        var deref = this;
        if (!value.IsEmpty)
        {
            if (!deref.IsEmpty && value.Length <= deref.Length)
            {
                result = deref.UnsafeSpan.EndsWith(value.UnsafeSpan);
            }
            else result = false;
        }
        else result = true;

        return result;
    }

    public bool EndsWith(ReadOnlySpan<byte> value)
    {
        bool result;
        var deref = this;
        if (!deref.IsEmpty && value.Length <= deref.Length)
        {
            result = deref.UnsafeSpan.EndsWith(value);
        }
        else result = value.IsEmpty;

        return result;
    }

    public bool EndsWith<T>(byte value, T comparer)
        where T : IU8EndsWithOperator
    {
        return comparer.EndsWith(this, value);
    }

    public bool EndsWith<T>(char value, T comparer)
        where T : IU8EndsWithOperator
    {
        ThrowHelpers.CheckSurrogate(value);

        return char.IsAscii(value)
            ? EndsWith((byte)value, comparer)
            : EndsWith(new U8Scalar(value, checkAscii: false).AsSpan(), comparer);
    }

    public bool EndsWith<T>(Rune value, T comparer)
        where T : IU8EndsWithOperator
    {
        return value.IsAscii
            ? EndsWith((byte)value.Value, comparer)
            : EndsWith(new U8Scalar(value, checkAscii: false).AsSpan(), comparer);
    }

    public bool EndsWith<T>(U8String value, T comparer)
        where T : IU8EndsWithOperator
    {
        return comparer.EndsWith(this, value);
    }

    public bool EndsWith<T>(ReadOnlySpan<byte> value, T comparer)
        where T : IU8EndsWithOperator
    {
        return comparer.EndsWith(this, value);
    }

    public int IndexOf(byte value)
    {
        int result;
        var deref = this;
        if (!deref.IsEmpty)
        {
            result = deref.UnsafeSpan.IndexOf(value);
        }
        else result = -1;

        return result;
    }

    public int IndexOf(char value)
    {
        ThrowHelpers.CheckSurrogate(value);

        return char.IsAscii(value)
            ? IndexOf((byte)value)
            : IndexOf(new U8Scalar(value, checkAscii: false).AsSpan());
    }

    public int IndexOf(Rune value)
    {
        return value.IsAscii
            ? IndexOf((byte)value.Value)
            : IndexOf(new U8Scalar(value, checkAscii: false).AsSpan());
    }

    public int IndexOf(U8String value)
    {
        int result;
        var deref = this;
        if (!value.IsEmpty)
        {
            if (!deref.IsEmpty)
            {
                result = deref.UnsafeSpan.IndexOf(value.UnsafeSpan);
            }
            else result = -1;
        }
        else result = 0;

        return result;
    }

    public int IndexOf(ReadOnlySpan<byte> value)
    {
        int result;
        var deref = this;
        if (!deref.IsEmpty)
        {
            result = deref.UnsafeSpan.IndexOf(value);
        }
        else result = value.IsEmpty ? 0 : -1;

        return result;
    }

    public int IndexOf<T>(byte value, T comparer)
        where T : IU8IndexOfOperator
    {
        return comparer.IndexOf(this, value).Offset;
    }

    public int IndexOf<T>(char value, T comparer)
        where T : IU8IndexOfOperator
    {
        ThrowHelpers.CheckSurrogate(value);

        return char.IsAscii(value)
            ? IndexOf((byte)value, comparer)
            : IndexOf(new U8Scalar(value, checkAscii: false).AsSpan(), comparer);
    }

    public int IndexOf<T>(Rune value, T comparer)
        where T : IU8IndexOfOperator
    {
        return value.IsAscii
            ? IndexOf((byte)value.Value, comparer)
            : IndexOf(new U8Scalar(value, checkAscii: false).AsSpan(), comparer);
    }

    public int IndexOf<T>(U8String value, T comparer)
        where T : IU8IndexOfOperator
    {
        return comparer.IndexOf(this, value).Offset;
    }

    public int IndexOf<T>(ReadOnlySpan<byte> value, T comparer)
        where T : IU8IndexOfOperator
    {
        return comparer.IndexOf(this, value).Offset;
    }

    public int LastIndexOf(byte value)
    {
        int result;
        var deref = this;
        if (!deref.IsEmpty)
        {
            result = deref.UnsafeSpan.LastIndexOf(value);
        }
        else result = -1;

        return result;
    }

    public int LastIndexOf(char value)
    {
        ThrowHelpers.CheckSurrogate(value);

        return char.IsAscii(value)
            ? LastIndexOf((byte)value)
            : LastIndexOf(new U8Scalar(value, checkAscii: false).AsSpan());
    }

    public int LastIndexOf(Rune value)
    {
        return value.IsAscii
            ? LastIndexOf((byte)value.Value)
            : LastIndexOf(new U8Scalar(value, checkAscii: false).AsSpan());
    }

    public int LastIndexOf(U8String value)
    {
        int result;
        var deref = this;
        if (!value.IsEmpty)
        {
            if (!deref.IsEmpty)
            {
                result = deref.UnsafeSpan.LastIndexOf(value.UnsafeSpan);
            }
            else result = -1;
        }
        else result = 0;

        return result;
    }

    public int LastIndexOf(ReadOnlySpan<byte> value)
    {
        int result;
        var deref = this;
        if (!deref.IsEmpty)
        {
            result = deref.UnsafeSpan.LastIndexOf(value);
        }
        else result = value.IsEmpty ? 0 : -1;

        return result;
    }

    public int LastIndexOf<T>(byte value, T comparer)
        where T : IU8LastIndexOfOperator
    {
        return comparer.LastIndexOf(this, value).Offset;
    }

    public int LastIndexOf<T>(char value, T comparer)
        where T : IU8LastIndexOfOperator
    {
        ThrowHelpers.CheckSurrogate(value);

        return char.IsAscii(value)
            ? LastIndexOf((byte)value, comparer)
            : LastIndexOf(new U8Scalar(value, checkAscii: false).AsSpan(), comparer);
    }

    public int LastIndexOf<T>(Rune value, T comparer)
        where T : IU8LastIndexOfOperator
    {
        return value.IsAscii
            ? LastIndexOf((byte)value.Value, comparer)
            : LastIndexOf(new U8Scalar(value, checkAscii: false).AsSpan(), comparer);
    }

    public int LastIndexOf<T>(U8String value, T comparer)
        where T : IU8LastIndexOfOperator
    {
        return comparer.LastIndexOf(this, value).Offset;
    }

    public int LastIndexOf<T>(ReadOnlySpan<byte> value, T comparer)
        where T : IU8LastIndexOfOperator
    {
        return comparer.LastIndexOf(this, value).Offset;
    }
}
