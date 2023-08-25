using System.Text;
using U8Primitives.Abstractions;
using U8Primitives.InteropServices;

namespace U8Primitives;

public readonly partial struct U8String
{
    public bool Contains(byte value) => U8Searching.Contains(this, value);

    // TODO: Decide whether to throw on surrogate chars here or just return false (as right now)
    public bool Contains(char value) => U8Searching.Contains(this, value);

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

    public bool StartsWith(byte value)
    {
        var span = AsSpan();
        return span.Length > 0 && span[0] == value;
    }

    public bool StartsWith(char value) => char.IsAscii(value)
        ? StartsWith((byte)value)
        : StartsWith(U8Scalar.Create(value, checkAscii: false).AsSpan());

    public bool StartsWith(Rune value) => value.IsAscii
        ? StartsWith((byte)value.Value)
        : StartsWith(U8Scalar.Create(value, checkAscii: false).AsSpan());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool StartsWith(U8String value) => AsSpan().StartsWith(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool StartsWith(ReadOnlySpan<byte> value) => AsSpan().StartsWith(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EndsWith(byte value)
    {
        var span = AsSpan();
        return span.Length > 0 && span[^1] == value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EndsWith(char value) => char.IsAscii(value)
        ? EndsWith((byte)value)
        : EndsWith(U8Scalar.Create(value, checkAscii: false).AsSpan());

    public bool EndsWith(Rune value) => value.IsAscii
        ? EndsWith((byte)value.Value)
        : EndsWith(U8Scalar.Create(value, checkAscii: false).AsSpan());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EndsWith(U8String value) => AsSpan().EndsWith(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EndsWith(ReadOnlySpan<byte> value) => AsSpan().EndsWith(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    public int IndexOf(byte value) => U8Searching.IndexOf(this, value).Offset;

    public int IndexOf(char value) => U8Searching.IndexOf(this, value).Offset;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int LastIndexOf(byte value) => AsSpan().LastIndexOf(value);

    public int LastIndexOf(char value)
    {
        return AsSpan().LastIndexOf(U8Scalar.Create(value).AsSpan());
    }

    public int LastIndexOf(Rune value)
    {
        return AsSpan().LastIndexOf(U8Scalar.Create(value).AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int LastIndexOf(U8String value) => AsSpan().LastIndexOf(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int LastIndexOf(ReadOnlySpan<byte> value) => AsSpan().LastIndexOf(value);
}
