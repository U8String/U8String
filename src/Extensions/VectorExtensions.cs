using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;

namespace U8Primitives;

internal static class VectorExtensions
{
    /// <summary>
    /// Assumes well-formed UTF-8 byte vector.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector256<byte> ToLowerAscii_AdvSimd(this Vector256<byte> ascii)
    {
        var (lo, hi) = ascii;

        return Vector256.Create(
            lo.ToLowerAscii_AdvSimd(),
            hi.ToLowerAscii_AdvSimd());
    }

    /// <inheritdoc cref="ToLowerAscii_AdvSimd(Vector256{byte})"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector128<byte> ToLowerAscii_AdvSimd(this Vector128<byte> ascii)
    {
        // Does not work???
        var lut1 = Vector128.Create((byte)
            0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28,
            0x29, 0x2A, 0x2B, 0x2D, 0x2E, 0x2F, 0x30, 0x31);

        var lut2 = Vector128.Create((byte)
            0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39,
            0x40, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);

        var shift = Vector128.Create((byte)'A');

        var original = ascii - shift;
        var mapped = AdvSimd.Arm64.VectorTableLookupExtension(
            Vector128<byte>.Zero, (lut1, lut2), original);

        return Vector128.Max(mapped + shift, ascii);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int CountMatches(this Vector256<byte> mask)
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
