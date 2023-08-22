using System.Text;

using U8Primitives.InteropServices;

namespace U8Primitives;

public readonly partial struct U8String
{
    public bool Contains(byte value) => U8Searching.Contains(this, value);

    public bool Contains(char value) => U8Searching.Contains(this, value);

    public bool Contains(Rune value) => U8Searching.Contains(this, value);

    public bool Contains(U8String value) => U8Searching.Contains(this, value);

    public bool Contains(ReadOnlySpan<byte> value) => U8Searching.Contains(this, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool StartsWith(byte value)
    {
        var span = AsSpan();
        return span.Length > 0 && span[0] == value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool StartsWith(char value) => char.IsAscii(value)
        ? StartsWith((byte)value)
        : StartsWith(new Rune(value));

    public bool StartsWith(Rune value)
    {
        return AsSpan().StartsWith(U8Scalar.Create(value).AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool StartsWith(U8String value) => AsSpan().StartsWith(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool StartsWith(ReadOnlySpan<byte> value) => AsSpan().StartsWith(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool StartsWith<T>(U8String value, T comparer)
        where T : IEqualityComparer<U8String>
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
        : EndsWith(new Rune(value));

    public bool EndsWith(Rune value)
    {
        return AsSpan().EndsWith(U8Scalar.Create(value).AsSpan());
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOf(byte value) => AsSpan().IndexOf(value);

    public int IndexOf(Rune value)
    {
        return AsSpan().IndexOf(U8Scalar.Create(value).AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOf(U8String value) => AsSpan().IndexOf(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOf(ReadOnlySpan<byte> value) => AsSpan().IndexOf(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int LastIndexOf(byte value) => AsSpan().LastIndexOf(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int LastIndexOf(Rune value)
    {
        return AsSpan().LastIndexOf(U8Scalar.Create(value).AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int LastIndexOf(U8String value) => AsSpan().LastIndexOf(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int LastIndexOf(ReadOnlySpan<byte> value) => AsSpan().LastIndexOf(value);
}
