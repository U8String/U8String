using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace U8.InteropServices;

// TODO: Double-check to ensure zero-extension on all casts and math (after all, _length must never be negative)
#pragma warning disable IDE0032, RCS1085 // Use auto-implemented property. Why: readable layout
internal unsafe readonly partial struct NativeU8Span
{
    public static NativeU8Span Empty => default;

    readonly byte* _ptr;
    readonly nint _length;

    public nint Length => _length;

    internal Span<byte> UnsafeSpan
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => MemoryMarshal.CreateSpan(ref _ptr[0], (int)_length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> AsSpan()
    {
        return MemoryMarshal.CreateReadOnlySpan(ref _ptr[0], (int)_length);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly byte GetPinnableReference()
    {
        return ref _ptr[0];
    }

    public override string ToString()
    {
        return Encoding.UTF8.GetString(this);
    }
}
