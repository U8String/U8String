using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace U8Primitives; // .InteropServices?

// The best is yet to come :^)
// Q: store a bit in _length to indicate if it's null-terminated?
// Is it viable to do some tagged pointer shenanigans?
// Q: Consider some clever WeakReference handler with dealloc callback? What kind of object should I use for root?
// Q: UnmanagedU8String? How does CString look like?
// Q: NativeU8String<TAlloc>?
// Q: Always null-terminated?
// TODO: Double-check to ensure zero-extension on all casts and math (after all, _length must never be negative)
// TODO: Sanity of handling free(_ptr) seems to necessitate introducing U8Slice or U8Span after all...this is because
// _ptr must remain pointing to a start of the original allocation. Seriously consider Rust-like approach where
// 1) the main type is a string slice and 2) it can be implicitly converted to and implements the majority of the API surface,
// so worst case it is possible to just e.g. U8String[..].Split(...) or U8String.AsSpan/Slice().Split(...).
// Alternatively, this can be handled through generic extension methods but that would make it incompatible with ref structs.
// Another option: refactor abstractions into trait-like interfaces and have IUncheckedSliceable<TSelf>, ISpanConvertible<TElement>, etc.
#pragma warning disable IDE0032, RCS1085 // Use auto-implemented property. Why: readable layout
public unsafe readonly partial struct NativeU8String : IDisposable
{
    public static NativeU8String Empty => default;

    readonly byte* _ptr;
    readonly nint _length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal NativeU8String(byte* ptr, nint length)
    {
        _ptr = ptr;
        _length = length;
    }

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
        if (_ptr != null) NativeMemory.Free(_ptr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<byte>(NativeU8String str) => str.AsSpan();
}
