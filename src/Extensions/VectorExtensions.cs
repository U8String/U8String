using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace U8Primitives;

internal static class VectorExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int CountMatches(this Vector256<byte> mask)
    {
        if (Vector256.IsHardwareAccelerated)
        {
            return BitOperations.PopCount(mask.ExtractMostSignificantBits());
        }

        var upper = mask.GetUpper();
        var lower = mask.GetLower();

        int upperCount, lowerCount;
        if (AdvSimd.Arm64.IsSupported)
        {
            var upperMatches = AdvSimd
                .ShiftRightLogicalNarrowingLower(upper.AsUInt16(), 4)
                .AsUInt64();
            var lowerMatches = AdvSimd
                .ShiftRightLogicalNarrowingLower(lower.AsUInt16(), 4)
                .AsUInt64();
            upperCount = BitOperations.PopCount(upperMatches.ToScalar()) >> 2;
            lowerCount = BitOperations.PopCount(lowerMatches.ToScalar()) >> 2;
        }
        else
        {
            var upperMatches = upper.ExtractMostSignificantBits();
            var lowerMatches = lower.ExtractMostSignificantBits();
            upperCount = BitOperations.PopCount(upperMatches);
            lowerCount = BitOperations.PopCount(lowerMatches);
        }

        return upperCount + lowerCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int CountMatches(this Vector128<byte> mask)
    {
        // Is there really no way to use AddAcross here?
        if (AdvSimd.Arm64.IsSupported)
        {
            var matches = AdvSimd
                .ShiftRightLogicalNarrowingLower(mask.AsUInt16(), 4)
                .AsUInt64()
                .ToScalar();
            return BitOperations.PopCount(matches) >> 2;
        }

        return BitOperations.PopCount(mask.ExtractMostSignificantBits());
    }
}
