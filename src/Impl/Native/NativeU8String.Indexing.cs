namespace U8Primitives;

public readonly unsafe partial struct NativeU8String
{
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

    public ref readonly byte this[Index index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var idx = index.GetOffset((int)_length);
            if (idx < 0 || (nint)(uint)idx >= _length)
            {
                ThrowHelpers.IndexOutOfRange();
            }

            return ref _ptr[idx];
        }
    }
}