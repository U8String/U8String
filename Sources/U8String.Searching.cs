using System.Text;
using U8Primitives.Abstractions;
using U8Primitives.InteropServices;

namespace U8Primitives;

public readonly partial struct U8String
{
    public bool Contains(byte value) => U8Searching.Contains(this, value);

    // TODO: Decide whether to throw on surrogate chars here or just return false (as right now)
    public bool Contains(char value)
    {
        if (char.IsSurrogate(value))
        {
            // TODO: EH UX
            ThrowHelpers.ArgumentOutOfRange();
        }

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
        if (char.IsSurrogate(value))
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        return char.IsAscii(value)
            ? StartsWith((byte)value)
            : StartsWith(U8Scalar.Create(value, checkAscii: false).AsSpan());
    }

    public bool StartsWith(Rune value) => value.IsAscii
        ? StartsWith((byte)value.Value)
        : StartsWith(U8Scalar.Create(value, checkAscii: false).AsSpan());

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

    // !! Invalid Code for invariant comparison on non-matching lengths !!
    public bool StartsWith<T>(U8String value, T comparer)
        where T : IU8EqualityComparer
    {
        var deref = this;
        if (deref.Length >= value.Length)
        {
            return U8Marshal
                .Slice(deref, 0, value.Length)
                .Equals(value, comparer);
        }

        return false;
    }

    public bool StartsWith<T>(ReadOnlySpan<byte> value, T comparer)
        where T : IU8EqualityComparer
    {
        var deref = this;
        if (deref.Length >= value.Length)
        {
            return U8Marshal
                .Slice(deref, 0, value.Length)
                .Equals(value, comparer);
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EndsWith(byte value)
    {
        return Length > 0 && UnsafeRefAdd(Length - 1) == value;
    }

    public bool EndsWith(char value)
    {
        if (char.IsSurrogate(value))
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        return char.IsAscii(value)
            ? EndsWith((byte)value)
            : EndsWith(U8Scalar.Create(value, checkAscii: false).AsSpan());
    }

    public bool EndsWith(Rune value) => value.IsAscii
        ? EndsWith((byte)value.Value)
        : EndsWith(U8Scalar.Create(value, checkAscii: false).AsSpan());

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

    // !! Invalid Code for invariant comparison on non-matching lengths !!
    public bool EndsWith<T>(U8String value, T comparer)
        where T : IEqualityComparer<U8String>
    {
        var deref = this;
        if (deref.Length >= value.Length)
        {
            return U8Marshal
                .Slice(deref, deref.Length - value.Length)
                .Equals(value, comparer);
        }

        return false;
    }

    public bool EndsWith<T>(ReadOnlySpan<byte> value, T comparer)
        where T : IU8EqualityComparer
    {
        var deref = this;
        if (deref.Length >= value.Length)
        {
            return U8Marshal
                .Slice(deref, deref.Length - value.Length)
                .Equals(value, comparer);
        }

        return false;
    }

    public int IndexOf(byte value) => U8Searching.IndexOf(this, value).Offset;

    public int IndexOf(char value)
    {
        if (char.IsSurrogate(value))
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

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
        if (char.IsSurrogate(value))
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

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
        if (char.IsSurrogate(value))
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

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
        if (char.IsSurrogate(value))
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

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
