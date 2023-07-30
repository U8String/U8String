using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace U8Primitives;

static class RuneExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ReadOnlySpan<byte> ToUtf8Unsafe(
        this Rune value,
        [UnscopedRef] out uint _)
    {
        _ = default;
        var bytes = _.AsBytes();
        var length = value.ToUtf8Unsafe(bytes);

        return bytes.SliceUnsafe(0, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe int ToUtf8Unsafe(this Rune value, Span<byte> destination)
    {
        fixed (byte* ptr = &MemoryMarshal.GetReference(destination))
        {
            var scalar = (uint)value.Value;

            // And I pray: unlimited optimization works
            // (dear compiler please fold this)
            if (scalar <= 0x7F)
            {
                ptr[0] = (byte)scalar;
                return 1;
            }
            else if (scalar <= 0x7FFu)
            {
                ptr[0] = (byte)((scalar + (0b110u << 11)) >> 6);
                ptr[1] = (byte)((scalar & 0x3Fu) + 0x80u);
                return 2;
            }
            else if (scalar <= 0xFFFFu)
            {
                ptr[0] = (byte)((scalar + (0b1110 << 16)) >> 12);
                ptr[1] = (byte)(((scalar & (0x3Fu << 6)) >> 6) + 0x80u);
                ptr[2] = (byte)((scalar & 0x3Fu) + 0x80u);
                return 3;
            }
            else
            {
                ptr[0] = (byte)((scalar + (0b11110 << 21)) >> 18);
                ptr[1] = (byte)(((scalar & (0x3Fu << 12)) >> 12) + 0x80u);
                ptr[2] = (byte)(((scalar & (0x3Fu << 6)) >> 6) + 0x80u);
                ptr[3] = (byte)((scalar & 0x3Fu) + 0x80u);
                return 4;
            }
        }
    }
}