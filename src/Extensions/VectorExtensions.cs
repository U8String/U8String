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

        var lower = mask.GetLower();
        var upper = mask.GetUpper();
        var lowerCount = CountMatches(lower);
        var upperCount = CountMatches(upper);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector256<T> Gt<T>(this Vector256<T> left, Vector256<T> right)
    {
        return Vector256.GreaterThan(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector128<T> Gt<T>(this Vector128<T> left, Vector128<T> right)
    {
        return Vector128.GreaterThan(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector256<T> Gte<T>(this Vector256<T> left, Vector256<T> right)
    {
        return Vector256.GreaterThanOrEqual(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector128<T> Gte<T>(this Vector128<T> left, Vector128<T> right)
    {
        return Vector128.GreaterThanOrEqual(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector256<T> Lt<T>(this Vector256<T> left, Vector256<T> right)
    {
        return Vector256.LessThan(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector128<T> Lt<T>(this Vector128<T> left, Vector128<T> right)
    {
        return Vector128.LessThan(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector256<T> Lte<T>(this Vector256<T> left, Vector256<T> right)
    {
        return Vector256.LessThanOrEqual(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector128<T> Lte<T>(this Vector128<T> left, Vector128<T> right)
    {
        return Vector128.LessThanOrEqual(left, right);
    }
}
