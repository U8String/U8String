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

        var (lower, upper) = mask;
        var lowerCount = CountMatches(lower);
        var upperCount = CountMatches(upper);

        return upperCount + lowerCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int CountMatches<T>(this Vector128<T> mask)
    {
        if (AdvSimd.Arm64.IsSupported)
        {
            return AdvSimd.Arm64
                .AddAcross(AdvSimd.PopCount(mask.AsByte()))
                .ToScalar() / (8 * Unsafe.SizeOf<T>());
        }

        return BitOperations.PopCount(mask.ExtractMostSignificantBits());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int CountMatches<T>(this Vector64<T> mask)
    {
        return AdvSimd.Arm64
            .AddAcross(AdvSimd.PopCount(mask.AsByte()))
            .ToScalar() / (8 * Unsafe.SizeOf<T>());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Deconstruct<T>(
        this Vector256<T> vector, out Vector128<T> lo, out Vector128<T> hi)
    {
        lo = vector.GetLower();
        hi = vector.GetUpper();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector256<T> Eq<T>(this Vector256<T> left, Vector256<T> right)
    {
        return Vector256.Equals(left, right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector128<T> Eq<T>(this Vector128<T> left, Vector128<T> right)
    {
        return Vector128.Equals(left, right);
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
