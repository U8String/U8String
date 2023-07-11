using System.Buffers;

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

    /// <summary>
    /// Returns a substring of this <see cref="U8String"/> instance.
    /// </summary>
    /// <param name="range">The range of the new substring.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// start, length, or start + length is not in the range of text.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The resulting substring is not a valid UTF-8 string.
    /// </exception>
    public U8String this[Range range]
    {
        // TODO: Not great, not terrible. Try to make it good.
        get
        {
            var (offset, length) = range.GetOffsetAndLength((int)_length);
            var result = new U8String(_value, _offset + (uint)offset, (uint)length);

            // Drop the reference if the result is empty
            if (result.IsEmpty)
            {
                return default;
            }

            if (Rune.DecodeFromUtf8(result, out var _, out var _) is OperationStatus.InvalidData ||
                Rune.DecodeLastFromUtf8(result, out var _, out var _) is OperationStatus.InvalidData)
            {
                // TODO: Exception message UX
                ThrowHelpers.InvalidUtf8();
            }

            return result;
        }
    }
}
