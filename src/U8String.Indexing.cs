namespace U8Primitives;

public readonly partial struct U8String
{
    public byte this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)index >= Length)
            {
                ThrowHelpers.IndexOutOfRange();
            }

            return IndexUnsafe(index);
        }
    }

    public byte this[Index index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var i = index.GetOffset(Length);
            if ((uint)i >= _length)
            {
                ThrowHelpers.ArgumentOutOfRange();
            }

            return IndexUnsafe(i);
        }
    }

    public U8String this[Range range]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var (offset, length) = range.GetOffsetAndLength((int)_length);

            return new U8String(_value, _offset + (uint)offset, (uint)length);
        }
    }
}
