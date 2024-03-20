using System.Runtime.InteropServices;

namespace U8.InteropServices;

internal unsafe readonly partial struct NativeU8Span
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal NativeU8Span(byte* ptr, nint length)
    {
        _ptr = length > 0 ? ptr : null;
        _length = length;
    }
}
