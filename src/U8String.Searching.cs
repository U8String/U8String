using System.Text;

namespace U8Primitives;

public readonly partial struct U8String
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(byte value) => AsSpan().Contains(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Contains(char item) => char.IsAscii(item)
        ? Contains((byte)item)
        : Contains(new Rune(item));

    public bool Contains(Rune value)
    {
        return AsSpan().IndexOf(value.ToUtf8Unsafe(out _)) >= 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(U8String value) => AsSpan().IndexOf(value) >= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(ReadOnlySpan<byte> value) => AsSpan().IndexOf(value) >= 0;

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
        return AsSpan().StartsWith(value.ToUtf8Unsafe(out _));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool StartsWith(U8String value) => AsSpan().StartsWith(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool StartsWith(ReadOnlySpan<byte> value) => AsSpan().StartsWith(value);

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
        return AsSpan().EndsWith(value.ToUtf8Unsafe(out _));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EndsWith(U8String value) => AsSpan().EndsWith(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EndsWith(ReadOnlySpan<byte> value) => AsSpan().EndsWith(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOf(byte value) => AsSpan().IndexOf(value);

    public int IndexOf(Rune value)
    {
        return AsSpan().IndexOf(value.ToUtf8Unsafe(out _));
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
        return AsSpan().LastIndexOf(value.ToUtf8Unsafe(out _));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int LastIndexOf(U8String value) => AsSpan().LastIndexOf(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int LastIndexOf(ReadOnlySpan<byte> value) => AsSpan().LastIndexOf(value);
}
