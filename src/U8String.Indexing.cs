using System.Runtime.CompilerServices;

namespace U8Primitives;

public readonly partial struct U8String
{
    public byte this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)index >= (uint)_length)
            {
                ThrowHelpers.ArgumentOutOfRange();
            }

            return IndexUnsafe(index);
        }
    }

    public byte this[Index index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var i = index.GetOffset(_length);
            if ((uint)i >= (uint)_length)
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
            var (offset, length) = range.GetOffsetAndLength(_length);

            return new U8String(_value, _offset + offset, length);
        }
    }
}
