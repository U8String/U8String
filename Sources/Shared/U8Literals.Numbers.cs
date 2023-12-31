using System.Diagnostics;
using System.Runtime.InteropServices;

namespace U8.Shared;

static partial class U8Literals
{
    internal static class Numbers
    {
        internal const int Length = 256;

        internal static readonly U8String[] Values =
        [
            new U8String([(byte)'0', 0], 0, 1),
            new U8String([(byte)'1', 0], 0, 1),
            new U8String([(byte)'2', 0], 0, 1),
            new U8String([(byte)'3', 0], 0, 1),
            new U8String([(byte)'4', 0], 0, 1),
            new U8String([(byte)'5', 0], 0, 1),
            new U8String([(byte)'6', 0], 0, 1),
            new U8String([(byte)'7', 0], 0, 1),
            new U8String([(byte)'8', 0], 0, 1),
            new U8String([(byte)'9', 0], 0, 1),
            new U8String([(byte)'1', (byte)'0', 0], 0, 2),
            new U8String([(byte)'1', (byte)'1', 0], 0, 2),
            new U8String([(byte)'1', (byte)'2', 0], 0, 2),
            new U8String([(byte)'1', (byte)'3', 0], 0, 2),
            new U8String([(byte)'1', (byte)'4', 0], 0, 2),
            new U8String([(byte)'1', (byte)'5', 0], 0, 2),
            new U8String([(byte)'1', (byte)'6', 0], 0, 2),
            new U8String([(byte)'1', (byte)'7', 0], 0, 2),
            new U8String([(byte)'1', (byte)'8', 0], 0, 2),
            new U8String([(byte)'1', (byte)'9', 0], 0, 2),
            new U8String([(byte)'2', (byte)'0', 0], 0, 2),
            new U8String([(byte)'2', (byte)'1', 0], 0, 2),
            new U8String([(byte)'2', (byte)'2', 0], 0, 2),
            new U8String([(byte)'2', (byte)'3', 0], 0, 2),
            new U8String([(byte)'2', (byte)'4', 0], 0, 2),
            new U8String([(byte)'2', (byte)'5', 0], 0, 2),
            new U8String([(byte)'2', (byte)'6', 0], 0, 2),
            new U8String([(byte)'2', (byte)'7', 0], 0, 2),
            new U8String([(byte)'2', (byte)'8', 0], 0, 2),
            new U8String([(byte)'2', (byte)'9', 0], 0, 2),
            new U8String([(byte)'3', (byte)'0', 0], 0, 2),
            new U8String([(byte)'3', (byte)'1', 0], 0, 2),
            new U8String([(byte)'3', (byte)'2', 0], 0, 2),
            new U8String([(byte)'3', (byte)'3', 0], 0, 2),
            new U8String([(byte)'3', (byte)'4', 0], 0, 2),
            new U8String([(byte)'3', (byte)'5', 0], 0, 2),
            new U8String([(byte)'3', (byte)'6', 0], 0, 2),
            new U8String([(byte)'3', (byte)'7', 0], 0, 2),
            new U8String([(byte)'3', (byte)'8', 0], 0, 2),
            new U8String([(byte)'3', (byte)'9', 0], 0, 2),
            new U8String([(byte)'4', (byte)'0', 0], 0, 2),
            new U8String([(byte)'4', (byte)'1', 0], 0, 2),
            new U8String([(byte)'4', (byte)'2', 0], 0, 2),
            new U8String([(byte)'4', (byte)'3', 0], 0, 2),
            new U8String([(byte)'4', (byte)'4', 0], 0, 2),
            new U8String([(byte)'4', (byte)'5', 0], 0, 2),
            new U8String([(byte)'4', (byte)'6', 0], 0, 2),
            new U8String([(byte)'4', (byte)'7', 0], 0, 2),
            new U8String([(byte)'4', (byte)'8', 0], 0, 2),
            new U8String([(byte)'4', (byte)'9', 0], 0, 2),
            new U8String([(byte)'5', (byte)'0', 0], 0, 2),
            new U8String([(byte)'5', (byte)'1', 0], 0, 2),
            new U8String([(byte)'5', (byte)'2', 0], 0, 2),
            new U8String([(byte)'5', (byte)'3', 0], 0, 2),
            new U8String([(byte)'5', (byte)'4', 0], 0, 2),
            new U8String([(byte)'5', (byte)'5', 0], 0, 2),
            new U8String([(byte)'5', (byte)'6', 0], 0, 2),
            new U8String([(byte)'5', (byte)'7', 0], 0, 2),
            new U8String([(byte)'5', (byte)'8', 0], 0, 2),
            new U8String([(byte)'5', (byte)'9', 0], 0, 2),
            new U8String([(byte)'6', (byte)'0', 0], 0, 2),
            new U8String([(byte)'6', (byte)'1', 0], 0, 2),
            new U8String([(byte)'6', (byte)'2', 0], 0, 2),
            new U8String([(byte)'6', (byte)'3', 0], 0, 2),
            new U8String([(byte)'6', (byte)'4', 0], 0, 2),
            new U8String([(byte)'6', (byte)'5', 0], 0, 2),
            new U8String([(byte)'6', (byte)'6', 0], 0, 2),
            new U8String([(byte)'6', (byte)'7', 0], 0, 2),
            new U8String([(byte)'6', (byte)'8', 0], 0, 2),
            new U8String([(byte)'6', (byte)'9', 0], 0, 2),
            new U8String([(byte)'7', (byte)'0', 0], 0, 2),
            new U8String([(byte)'7', (byte)'1', 0], 0, 2),
            new U8String([(byte)'7', (byte)'2', 0], 0, 2),
            new U8String([(byte)'7', (byte)'3', 0], 0, 2),
            new U8String([(byte)'7', (byte)'4', 0], 0, 2),
            new U8String([(byte)'7', (byte)'5', 0], 0, 2),
            new U8String([(byte)'7', (byte)'6', 0], 0, 2),
            new U8String([(byte)'7', (byte)'7', 0], 0, 2),
            new U8String([(byte)'7', (byte)'8', 0], 0, 2),
            new U8String([(byte)'7', (byte)'9', 0], 0, 2),
            new U8String([(byte)'8', (byte)'0', 0], 0, 2),
            new U8String([(byte)'8', (byte)'1', 0], 0, 2),
            new U8String([(byte)'8', (byte)'2', 0], 0, 2),
            new U8String([(byte)'8', (byte)'3', 0], 0, 2),
            new U8String([(byte)'8', (byte)'4', 0], 0, 2),
            new U8String([(byte)'8', (byte)'5', 0], 0, 2),
            new U8String([(byte)'8', (byte)'6', 0], 0, 2),
            new U8String([(byte)'8', (byte)'7', 0], 0, 2),
            new U8String([(byte)'8', (byte)'8', 0], 0, 2),
            new U8String([(byte)'8', (byte)'9', 0], 0, 2),
            new U8String([(byte)'9', (byte)'0', 0], 0, 2),
            new U8String([(byte)'9', (byte)'1', 0], 0, 2),
            new U8String([(byte)'9', (byte)'2', 0], 0, 2),
            new U8String([(byte)'9', (byte)'3', 0], 0, 2),
            new U8String([(byte)'9', (byte)'4', 0], 0, 2),
            new U8String([(byte)'9', (byte)'5', 0], 0, 2),
            new U8String([(byte)'9', (byte)'6', 0], 0, 2),
            new U8String([(byte)'9', (byte)'7', 0], 0, 2),
            new U8String([(byte)'9', (byte)'8', 0], 0, 2),
            new U8String([(byte)'9', (byte)'9', 0], 0, 2),
            new U8String([(byte)'1', (byte)'0', (byte)'0', 0], 0, 3),
            new U8String([(byte)'1', (byte)'0', (byte)'1', 0], 0, 3),
            new U8String([(byte)'1', (byte)'0', (byte)'2', 0], 0, 3),
            new U8String([(byte)'1', (byte)'0', (byte)'3', 0], 0, 3),
            new U8String([(byte)'1', (byte)'0', (byte)'4', 0], 0, 3),
            new U8String([(byte)'1', (byte)'0', (byte)'5', 0], 0, 3),
            new U8String([(byte)'1', (byte)'0', (byte)'6', 0], 0, 3),
            new U8String([(byte)'1', (byte)'0', (byte)'7', 0], 0, 3),
            new U8String([(byte)'1', (byte)'0', (byte)'8', 0], 0, 3),
            new U8String([(byte)'1', (byte)'0', (byte)'9', 0], 0, 3),
            new U8String([(byte)'1', (byte)'1', (byte)'0', 0], 0, 3),
            new U8String([(byte)'1', (byte)'1', (byte)'1', 0], 0, 3),
            new U8String([(byte)'1', (byte)'1', (byte)'2', 0], 0, 3),
            new U8String([(byte)'1', (byte)'1', (byte)'3', 0], 0, 3),
            new U8String([(byte)'1', (byte)'1', (byte)'4', 0], 0, 3),
            new U8String([(byte)'1', (byte)'1', (byte)'5', 0], 0, 3),
            new U8String([(byte)'1', (byte)'1', (byte)'6', 0], 0, 3),
            new U8String([(byte)'1', (byte)'1', (byte)'7', 0], 0, 3),
            new U8String([(byte)'1', (byte)'1', (byte)'8', 0], 0, 3),
            new U8String([(byte)'1', (byte)'1', (byte)'9', 0], 0, 3),
            new U8String([(byte)'1', (byte)'2', (byte)'0', 0], 0, 3),
            new U8String([(byte)'1', (byte)'2', (byte)'1', 0], 0, 3),
            new U8String([(byte)'1', (byte)'2', (byte)'2', 0], 0, 3),
            new U8String([(byte)'1', (byte)'2', (byte)'3', 0], 0, 3),
            new U8String([(byte)'1', (byte)'2', (byte)'4', 0], 0, 3),
            new U8String([(byte)'1', (byte)'2', (byte)'5', 0], 0, 3),
            new U8String([(byte)'1', (byte)'2', (byte)'6', 0], 0, 3),
            new U8String([(byte)'1', (byte)'2', (byte)'7', 0], 0, 3),
            new U8String([(byte)'1', (byte)'2', (byte)'8', 0], 0, 3),
            new U8String([(byte)'1', (byte)'2', (byte)'9', 0], 0, 3),
            new U8String([(byte)'1', (byte)'3', (byte)'0', 0], 0, 3),
            new U8String([(byte)'1', (byte)'3', (byte)'1', 0], 0, 3),
            new U8String([(byte)'1', (byte)'3', (byte)'2', 0], 0, 3),
            new U8String([(byte)'1', (byte)'3', (byte)'3', 0], 0, 3),
            new U8String([(byte)'1', (byte)'3', (byte)'4', 0], 0, 3),
            new U8String([(byte)'1', (byte)'3', (byte)'5', 0], 0, 3),
            new U8String([(byte)'1', (byte)'3', (byte)'6', 0], 0, 3),
            new U8String([(byte)'1', (byte)'3', (byte)'7', 0], 0, 3),
            new U8String([(byte)'1', (byte)'3', (byte)'8', 0], 0, 3),
            new U8String([(byte)'1', (byte)'3', (byte)'9', 0], 0, 3),
            new U8String([(byte)'1', (byte)'4', (byte)'0', 0], 0, 3),
            new U8String([(byte)'1', (byte)'4', (byte)'1', 0], 0, 3),
            new U8String([(byte)'1', (byte)'4', (byte)'2', 0], 0, 3),
            new U8String([(byte)'1', (byte)'4', (byte)'3', 0], 0, 3),
            new U8String([(byte)'1', (byte)'4', (byte)'4', 0], 0, 3),
            new U8String([(byte)'1', (byte)'4', (byte)'5', 0], 0, 3),
            new U8String([(byte)'1', (byte)'4', (byte)'6', 0], 0, 3),
            new U8String([(byte)'1', (byte)'4', (byte)'7', 0], 0, 3),
            new U8String([(byte)'1', (byte)'4', (byte)'8', 0], 0, 3),
            new U8String([(byte)'1', (byte)'4', (byte)'9', 0], 0, 3),
            new U8String([(byte)'1', (byte)'5', (byte)'0', 0], 0, 3),
            new U8String([(byte)'1', (byte)'5', (byte)'1', 0], 0, 3),
            new U8String([(byte)'1', (byte)'5', (byte)'2', 0], 0, 3),
            new U8String([(byte)'1', (byte)'5', (byte)'3', 0], 0, 3),
            new U8String([(byte)'1', (byte)'5', (byte)'4', 0], 0, 3),
            new U8String([(byte)'1', (byte)'5', (byte)'5', 0], 0, 3),
            new U8String([(byte)'1', (byte)'5', (byte)'6', 0], 0, 3),
            new U8String([(byte)'1', (byte)'5', (byte)'7', 0], 0, 3),
            new U8String([(byte)'1', (byte)'5', (byte)'8', 0], 0, 3),
            new U8String([(byte)'1', (byte)'5', (byte)'9', 0], 0, 3),
            new U8String([(byte)'1', (byte)'6', (byte)'0', 0], 0, 3),
            new U8String([(byte)'1', (byte)'6', (byte)'1', 0], 0, 3),
            new U8String([(byte)'1', (byte)'6', (byte)'2', 0], 0, 3),
            new U8String([(byte)'1', (byte)'6', (byte)'3', 0], 0, 3),
            new U8String([(byte)'1', (byte)'6', (byte)'4', 0], 0, 3),
            new U8String([(byte)'1', (byte)'6', (byte)'5', 0], 0, 3),
            new U8String([(byte)'1', (byte)'6', (byte)'6', 0], 0, 3),
            new U8String([(byte)'1', (byte)'6', (byte)'7', 0], 0, 3),
            new U8String([(byte)'1', (byte)'6', (byte)'8', 0], 0, 3),
            new U8String([(byte)'1', (byte)'6', (byte)'9', 0], 0, 3),
            new U8String([(byte)'1', (byte)'7', (byte)'0', 0], 0, 3),
            new U8String([(byte)'1', (byte)'7', (byte)'1', 0], 0, 3),
            new U8String([(byte)'1', (byte)'7', (byte)'2', 0], 0, 3),
            new U8String([(byte)'1', (byte)'7', (byte)'3', 0], 0, 3),
            new U8String([(byte)'1', (byte)'7', (byte)'4', 0], 0, 3),
            new U8String([(byte)'1', (byte)'7', (byte)'5', 0], 0, 3),
            new U8String([(byte)'1', (byte)'7', (byte)'6', 0], 0, 3),
            new U8String([(byte)'1', (byte)'7', (byte)'7', 0], 0, 3),
            new U8String([(byte)'1', (byte)'7', (byte)'8', 0], 0, 3),
            new U8String([(byte)'1', (byte)'7', (byte)'9', 0], 0, 3),
            new U8String([(byte)'1', (byte)'8', (byte)'0', 0], 0, 3),
            new U8String([(byte)'1', (byte)'8', (byte)'1', 0], 0, 3),
            new U8String([(byte)'1', (byte)'8', (byte)'2', 0], 0, 3),
            new U8String([(byte)'1', (byte)'8', (byte)'3', 0], 0, 3),
            new U8String([(byte)'1', (byte)'8', (byte)'4', 0], 0, 3),
            new U8String([(byte)'1', (byte)'8', (byte)'5', 0], 0, 3),
            new U8String([(byte)'1', (byte)'8', (byte)'6', 0], 0, 3),
            new U8String([(byte)'1', (byte)'8', (byte)'7', 0], 0, 3),
            new U8String([(byte)'1', (byte)'8', (byte)'8', 0], 0, 3),
            new U8String([(byte)'1', (byte)'8', (byte)'9', 0], 0, 3),
            new U8String([(byte)'1', (byte)'9', (byte)'0', 0], 0, 3),
            new U8String([(byte)'1', (byte)'9', (byte)'1', 0], 0, 3),
            new U8String([(byte)'1', (byte)'9', (byte)'2', 0], 0, 3),
            new U8String([(byte)'1', (byte)'9', (byte)'3', 0], 0, 3),
            new U8String([(byte)'1', (byte)'9', (byte)'4', 0], 0, 3),
            new U8String([(byte)'1', (byte)'9', (byte)'5', 0], 0, 3),
            new U8String([(byte)'1', (byte)'9', (byte)'6', 0], 0, 3),
            new U8String([(byte)'1', (byte)'9', (byte)'7', 0], 0, 3),
            new U8String([(byte)'1', (byte)'9', (byte)'8', 0], 0, 3),
            new U8String([(byte)'1', (byte)'9', (byte)'9', 0], 0, 3),
            new U8String([(byte)'2', (byte)'0', (byte)'0', 0], 0, 3),
            new U8String([(byte)'2', (byte)'0', (byte)'1', 0], 0, 3),
            new U8String([(byte)'2', (byte)'0', (byte)'2', 0], 0, 3),
            new U8String([(byte)'2', (byte)'0', (byte)'3', 0], 0, 3),
            new U8String([(byte)'2', (byte)'0', (byte)'4', 0], 0, 3),
            new U8String([(byte)'2', (byte)'0', (byte)'5', 0], 0, 3),
            new U8String([(byte)'2', (byte)'0', (byte)'6', 0], 0, 3),
            new U8String([(byte)'2', (byte)'0', (byte)'7', 0], 0, 3),
            new U8String([(byte)'2', (byte)'0', (byte)'8', 0], 0, 3),
            new U8String([(byte)'2', (byte)'0', (byte)'9', 0], 0, 3),
            new U8String([(byte)'2', (byte)'1', (byte)'0', 0], 0, 3),
            new U8String([(byte)'2', (byte)'1', (byte)'1', 0], 0, 3),
            new U8String([(byte)'2', (byte)'1', (byte)'2', 0], 0, 3),
            new U8String([(byte)'2', (byte)'1', (byte)'3', 0], 0, 3),
            new U8String([(byte)'2', (byte)'1', (byte)'4', 0], 0, 3),
            new U8String([(byte)'2', (byte)'1', (byte)'5', 0], 0, 3),
            new U8String([(byte)'2', (byte)'1', (byte)'6', 0], 0, 3),
            new U8String([(byte)'2', (byte)'1', (byte)'7', 0], 0, 3),
            new U8String([(byte)'2', (byte)'1', (byte)'8', 0], 0, 3),
            new U8String([(byte)'2', (byte)'1', (byte)'9', 0], 0, 3),
            new U8String([(byte)'2', (byte)'2', (byte)'0', 0], 0, 3),
            new U8String([(byte)'2', (byte)'2', (byte)'1', 0], 0, 3),
            new U8String([(byte)'2', (byte)'2', (byte)'2', 0], 0, 3),
            new U8String([(byte)'2', (byte)'2', (byte)'3', 0], 0, 3),
            new U8String([(byte)'2', (byte)'2', (byte)'4', 0], 0, 3),
            new U8String([(byte)'2', (byte)'2', (byte)'5', 0], 0, 3),
            new U8String([(byte)'2', (byte)'2', (byte)'6', 0], 0, 3),
            new U8String([(byte)'2', (byte)'2', (byte)'7', 0], 0, 3),
            new U8String([(byte)'2', (byte)'2', (byte)'8', 0], 0, 3),
            new U8String([(byte)'2', (byte)'2', (byte)'9', 0], 0, 3),
            new U8String([(byte)'2', (byte)'3', (byte)'0', 0], 0, 3),
            new U8String([(byte)'2', (byte)'3', (byte)'1', 0], 0, 3),
            new U8String([(byte)'2', (byte)'3', (byte)'2', 0], 0, 3),
            new U8String([(byte)'2', (byte)'3', (byte)'3', 0], 0, 3),
            new U8String([(byte)'2', (byte)'3', (byte)'4', 0], 0, 3),
            new U8String([(byte)'2', (byte)'3', (byte)'5', 0], 0, 3),
            new U8String([(byte)'2', (byte)'3', (byte)'6', 0], 0, 3),
            new U8String([(byte)'2', (byte)'3', (byte)'7', 0], 0, 3),
            new U8String([(byte)'2', (byte)'3', (byte)'8', 0], 0, 3),
            new U8String([(byte)'2', (byte)'3', (byte)'9', 0], 0, 3),
            new U8String([(byte)'2', (byte)'4', (byte)'0', 0], 0, 3),
            new U8String([(byte)'2', (byte)'4', (byte)'1', 0], 0, 3),
            new U8String([(byte)'2', (byte)'4', (byte)'2', 0], 0, 3),
            new U8String([(byte)'2', (byte)'4', (byte)'3', 0], 0, 3),
            new U8String([(byte)'2', (byte)'4', (byte)'4', 0], 0, 3),
            new U8String([(byte)'2', (byte)'4', (byte)'5', 0], 0, 3),
            new U8String([(byte)'2', (byte)'4', (byte)'6', 0], 0, 3),
            new U8String([(byte)'2', (byte)'4', (byte)'7', 0], 0, 3),
            new U8String([(byte)'2', (byte)'4', (byte)'8', 0], 0, 3),
            new U8String([(byte)'2', (byte)'4', (byte)'9', 0], 0, 3),
            new U8String([(byte)'2', (byte)'5', (byte)'0', 0], 0, 3),
            new U8String([(byte)'2', (byte)'5', (byte)'1', 0], 0, 3),
            new U8String([(byte)'2', (byte)'5', (byte)'2', 0], 0, 3),
            new U8String([(byte)'2', (byte)'5', (byte)'3', 0], 0, 3),
            new U8String([(byte)'2', (byte)'5', (byte)'4', 0], 0, 3),
            new U8String([(byte)'2', (byte)'5', (byte)'5', 0], 0, 3)
        ];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsInRange(nint value)
        {
            return (nuint)value < Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static U8String GetValueUnchecked(nint value)
        {
            Debug.Assert((nuint)value < Length);
            return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(Values), value);
        }
    }
}
