namespace U8.InteropServices;

internal unsafe readonly partial struct NativeU8String<T>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator NativeU8Span(NativeU8String<T> str) => new(str._ptr, str._length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<byte>(NativeU8String<T> str) => str.AsSpan();
}
