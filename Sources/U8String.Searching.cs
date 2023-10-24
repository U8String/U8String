using System.Text;
using U8Primitives.Abstractions;

namespace U8Primitives;

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
        if (!IsEmpty)
        {
            return UnsafeSpan.CommonPrefixLength(other);
        }
        
        return 0;
    }

    public bool Contains(byte value) => U8Searching.Contains(this, value);

    public bool Contains(char value)
    {
        ThrowHelpers.CheckSurrogate(value);

        return U8Searching.Contains(this, value);
    }

    public bool Contains(Rune value) => U8Searching.Contains(this, value);

    public bool Contains(U8String value) => U8Searching.Contains(this, value);

    public bool Contains(ReadOnlySpan<byte> value) => U8Searching.Contains(this, value);

    public bool Contains<T>(byte value, T comparer)
        where T : IU8ContainsOperator
    {
        return U8Searching.Contains(this, value, comparer);
    }

    public bool Contains<T>(char value, T comparer)
        where T : IU8ContainsOperator
    {
        ThrowHelpers.CheckSurrogate(value);

        return U8Searching.Contains(this, value, comparer);
    }

    public bool Contains<T>(Rune value, T comparer)
        where T : IU8ContainsOperator
    {
        return U8Searching.Contains(this, value, comparer);
    }

    public bool Contains<T>(U8String value, T comparer)
        where T : IU8ContainsOperator
    {
        return U8Searching.Contains(this, value, comparer);
    }

    public bool Contains<T>(ReadOnlySpan<byte> value, T comparer)
        where T : IU8ContainsOperator
    {
        return U8Searching.Contains(this, value, comparer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool StartsWith(byte value)
    {
        return Length > 0 && UnsafeRef == value;
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
        var deref = this;
        if (deref.Length >= value.Length)
        {
            if (deref.Length > 0)
            {
                return deref.UnsafeSpan
                    .SliceUnsafe(0, value.Length)
                    .SequenceEqual(value.UnsafeSpan);
            }

            return true;
        }

        return false;
    }

    public bool StartsWith(ReadOnlySpan<byte> value)
    {
        var deref = this;
        if (deref.Length >= value.Length)
        {
            if (deref.Length > 0)
            {
                return deref.UnsafeSpan
                    .SliceUnsafe(0, value.Length)
                    .SequenceEqual(value);
            }

            return true;
        }

        return false;
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
        return Length > 0 && UnsafeRefAdd(Length - 1) == value;
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
        var deref = this;
        if (deref.Length >= value.Length)
        {
            if (deref.Length > 0)
            {
                return deref.UnsafeSpan
                    .SliceUnsafe(deref.Length - value.Length)
                    .SequenceEqual(value.UnsafeSpan);
            }

            return true;
        }

        return false;
    }

    public bool EndsWith(ReadOnlySpan<byte> value)
    {
        var deref = this;
        if (deref.Length >= value.Length)
        {
            if (deref.Length > 0)
            {
                return deref.UnsafeSpan
                    .SliceUnsafe(deref.Length - value.Length)
                    .SequenceEqual(value);
            }

            return true;
        }

        return false;
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

    public int IndexOf(byte value) => U8Searching.IndexOf(this, value).Offset;

    public int IndexOf(char value)
    {
        ThrowHelpers.CheckSurrogate(value);

        return U8Searching.IndexOf(this, value).Offset;
    }

    public int IndexOf(Rune value) => U8Searching.IndexOf(this, value).Offset;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOf(U8String value) => U8Searching.IndexOf(this, value).Offset;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOf(ReadOnlySpan<byte> value) => U8Searching.IndexOf(this, value);

    public int IndexOf<T>(byte value, T comparer)
        where T : IU8IndexOfOperator
    {
        return U8Searching.IndexOf(this, value, comparer).Offset;
    }

    public int IndexOf<T>(char value, T comparer)
        where T : IU8IndexOfOperator
    {
        ThrowHelpers.CheckSurrogate(value);

        return U8Searching.IndexOf(this, value, comparer).Offset;
    }

    public int IndexOf<T>(Rune value, T comparer)
        where T : IU8IndexOfOperator
    {
        return U8Searching.IndexOf(this, value, comparer).Offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOf<T>(U8String value, T comparer)
        where T : IU8IndexOfOperator
    {
        return U8Searching.IndexOf(this, value, comparer).Offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOf<T>(ReadOnlySpan<byte> value, T comparer)
        where T : IU8IndexOfOperator
    {
        return U8Searching.IndexOf(this, value, comparer).Offset;
    }

    public int LastIndexOf(byte value) => U8Searching.LastIndexOf(this, value).Offset;

    public int LastIndexOf(char value)
    {
        ThrowHelpers.CheckSurrogate(value);

        return U8Searching.LastIndexOf(this, value).Offset;
    }

    public int LastIndexOf(Rune value) => U8Searching.LastIndexOf(this, value).Offset;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int LastIndexOf(U8String value) => U8Searching.LastIndexOf(this, value).Offset;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int LastIndexOf(ReadOnlySpan<byte> value) => U8Searching.LastIndexOf(this, value);

    public int LastIndexOf<T>(byte value, T comparer)
        where T : IU8LastIndexOfOperator
    {
        return U8Searching.LastIndexOf(this, value, comparer).Offset;
    }

    public int LastIndexOf<T>(char value, T comparer)
        where T : IU8LastIndexOfOperator
    {
        ThrowHelpers.CheckSurrogate(value);

        return U8Searching.LastIndexOf(this, value, comparer).Offset;
    }

    public int LastIndexOf<T>(Rune value, T comparer)
        where T : IU8LastIndexOfOperator
    {
        return U8Searching.LastIndexOf(this, value, comparer).Offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int LastIndexOf<T>(U8String value, T comparer)
        where T : IU8LastIndexOfOperator
    {
        return U8Searching.LastIndexOf(this, value, comparer).Offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int LastIndexOf<T>(ReadOnlySpan<byte> value, T comparer)
        where T : IU8LastIndexOfOperator
    {
        return U8Searching.LastIndexOf(this, value, comparer).Offset;
    }
}
