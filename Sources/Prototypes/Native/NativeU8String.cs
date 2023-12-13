using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace U8.InteropServices;

// TODO: Design choices
// - Immutable (ptr+len) vs mutable (ptr+len+capacity)
// - If NativeU8Span can be projected from mutable, it must not have use-after-free issues
// - Consider using class with object finalizer instead
// - Store additional state? Allow for dealloc callbacks?
internal unsafe readonly partial struct NativeU8String<T> : IDisposable
    where T : struct, IU8Allocator
{
    public static NativeU8String<T> Empty => default;

    readonly byte* _ptr;
    readonly nint _length;


    // TODO: Either switch to nuint or mask out the most significant bit
    // TODO: Proper converstion to int, exposed either via ShortLength or change this to NativeLength?
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

    public void Dispose()
    {
        if (_ptr is not null)
        {
            T.Free(_ptr);
        }
    }
}
