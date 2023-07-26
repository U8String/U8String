using System.Text;

namespace U8Primitives;

public readonly partial struct U8String
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(byte value) => AsSpan().Contains(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(Rune value)
    {
        var bytes = (stackalloc byte[4]);
        var length = value.EncodeToUtf8(bytes);

        return AsSpan().IndexOf(bytes[..length]) >= 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(U8String value) => AsSpan().IndexOf(value) >= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(ReadOnlySpan<byte> value) => AsSpan().IndexOf(value) >= 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool StartsWith(Rune value)
    {
        var bytes = (stackalloc byte[4]);
        var length = value.EncodeToUtf8(bytes);

        return AsSpan().StartsWith(bytes[..length]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool StartsWith(U8String value) => AsSpan().StartsWith(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool StartsWith(ReadOnlySpan<byte> value) => AsSpan().StartsWith(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EndsWith(Rune value)
    {
        var bytes = (stackalloc byte[4]);
        var length = value.EncodeToUtf8(bytes);

        return AsSpan().EndsWith(bytes[..length]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EndsWith(U8String value) => AsSpan().EndsWith(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EndsWith(ReadOnlySpan<byte> value) => AsSpan().EndsWith(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOf(byte value) => AsSpan().IndexOf(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOf(Rune value)
    {
        var bytes = (stackalloc byte[4]);
        var length = value.EncodeToUtf8(bytes);

        return AsSpan().IndexOf(bytes[..length]);
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
        var bytes = (stackalloc byte[4]);
        var length = value.EncodeToUtf8(bytes);

        return AsSpan().LastIndexOf(bytes[..length]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int LastIndexOf(U8String value) => AsSpan().LastIndexOf(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int LastIndexOf(ReadOnlySpan<byte> value) => AsSpan().LastIndexOf(value);
}
