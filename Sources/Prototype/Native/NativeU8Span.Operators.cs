namespace U8.InteropServices;

internal unsafe readonly partial struct NativeU8Span
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<byte>(NativeU8Span str) => str.AsSpan();
}
