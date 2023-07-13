using System.Buffers;
using System.Runtime.InteropServices;

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
            if ((uint)i >= LengthInner)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // TODO: Not great, not terrible. Try to make it good.
        get
        {
            var current = this;

            var (currOffset, currLength) = (current.Offset, current.Length);
            var (offset, length) = range.GetOffsetAndLength(currLength);

            // Drop the reference if the result is empty
            if (length is 0)
            {
                return default;
            }

            var value = MemoryMarshal.CreateReadOnlySpan(
                ref System.Runtime.CompilerServices.Unsafe.Add(ref current.FirstByte, offset), length);
            // This hurts a lot
            if (Rune.DecodeFromUtf8(value, out var _, out var _) is OperationStatus.InvalidData ||
                Rune.DecodeLastFromUtf8(value, out var _, out var _) is OperationStatus.InvalidData)
            {
                // TODO: Exception message UX
                ThrowHelpers.InvalidUtf8();
            }

            return new(current.Value, (uint)(currOffset + offset), (uint)length);
        }
    }
}
