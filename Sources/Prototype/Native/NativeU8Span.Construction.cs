using System.Runtime.InteropServices;

namespace U8Primitives.InteropServices;

internal unsafe readonly partial struct NativeU8Span
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal NativeU8Span(byte* ptr, nint length)
    {
        _ptr = length > 0 ? ptr : null;
        _length = length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NativeU8Span CreateFromPinnedUnchecked(ReadOnlySpan<byte> value)
    {
        var ptr = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(value));
        var length = (nint)(uint)value.Length;

        return new(ptr, length);
    }
}
