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
internal unsafe readonly struct NativeU8String
{
    readonly byte* _ptr;
    readonly nint _length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public NativeU8String(byte* ptr, nint length)
    {
        _ptr = ptr;
        _length = length;
    }

    public nint Length => _length;

    // TODO: Write an analyzer that warns against indexing with `int` which can overflow.
    public ref readonly byte this[nint index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((nuint)index >= (nuint)_length)
            {
                ThrowHelpers.IndexOutOfRange();
            }

            return ref _ptr[index];
        }
    }

    public NativeU8String this[Range range]
    {
        get
        {
            var source = this;
            var (start, length) = range.GetOffsetAndLength((int)source._length);

            if ((start > 0 && U8Info.IsContinuationByte(in source._ptr[start])) || (
                length < source.Length && U8Info.IsContinuationByte(in source._ptr[start + length])))
            {
                // TODO: Exception message UX
                ThrowHelpers.InvalidSplit();
            }

            return new(_ptr + start, length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> AsSpan()
    {
        return new(_ptr, (int)_length);
    }

    public NativeU8String Slice(nint start)
    {
        var source = this;
        // From ReadOnly/Span<T> Slice(int) implementation
        if ((nuint)start > (nuint)source.Length)
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        var length = source.Length - start;
        if (length > 0)
        {
            if (U8Info.IsContinuationByte(in source._ptr[start]))
            {
                ThrowHelpers.InvalidSplit();
            }

            return new(source._ptr + start, length);
        }

        return default;
    }

    public NativeU8String Slice(nint start, nint length)
    {
        var source = this;
        // From ReadOnly/Span<T> Slice(int, int) implementation
        if ((nuint)start + (nuint)length > (nuint)source.Length)
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        var result = default(NativeU8String);
        if (length > 0)
        {
            // TODO: If this is always null-terminated, should we skip length check?
            if ((start > 0 && U8Info.IsContinuationByte(in source._ptr[start])) || (
                length < source.Length && U8Info.IsContinuationByte(in source._ptr[start + length])))
            {
                // TODO: Exception message UX
                ThrowHelpers.InvalidSplit();
            }

            result = new(_ptr + start, length);
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<byte>(NativeU8String str) => str.AsSpan();

    public override string ToString()
    {
        return Encoding.UTF8.GetString(this);
    }
}
