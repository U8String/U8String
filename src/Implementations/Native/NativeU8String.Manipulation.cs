namespace U8Primitives;

public readonly unsafe partial struct NativeU8String
{
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
}