using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace U8Primitives;

internal static class VectorExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int CountMatches<T>(this Vector256<T> mask)
    {
        if (Vector256.IsHardwareAccelerated)
        {
            return BitOperations.PopCount(mask.ExtractMostSignificantBits());
        }
        else
        {
            return CountMatches(mask.GetLower()) + CountMatches(mask.GetUpper());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int CountMatches<T>(this Vector128<T> mask)
    {
        if (AdvSimd.Arm64.IsSupported)
        {
            var matches = AdvSimd
                .ShiftRightLogicalNarrowingLower(mask.AsUInt16(), 4)
                .AsUInt64()
                .ToScalar();
            return BitOperations.PopCount(matches) >> (int)((uint)BitOperations.PopCount(matches) / (4 * Unsafe.SizeOf<T>()));
        }
        else
        {
            return BitOperations.PopCount(mask.ExtractMostSignificantBits());
        }
    }
}
