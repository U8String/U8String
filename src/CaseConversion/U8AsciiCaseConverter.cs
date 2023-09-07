using System.Runtime.Intrinsics;

using U8Primitives.Abstractions;

namespace U8Primitives;

public readonly struct U8AsciiCaseConverter : IU8CaseConverter
{
    public static U8AsciiCaseConverter Instance => default;

    public (int ReplaceStart, int LowercaseLength) LowercaseHint(ReadOnlySpan<byte> source)
    {
        return (source.IndexOfAnyInRange((byte)'A', (byte)'Z'), source.Length);
    }

    public (int ReplaceStart, int UppercaseLength) UppercaseHint(ReadOnlySpan<byte> source)
    {
        return (source.IndexOfAnyInRange((byte)'a', (byte)'z'), source.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ToLower(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        if (destination.Length < source.Length)
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        ToLowerCore(ref source.AsRef(), ref destination.AsRef(), (nuint)source.Length);
        return source.Length;
    }

    // TODO: Consider for abstraction?
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector256<byte> ToLower(Vector256<byte> utf8)
    {
        var lower = Vector256.Create((byte)'A');
        var upper = Vector256.Create((byte)'Z');
        var mask = Vector256.Create((byte)0x20);

        var changeCase = mask
            & utf8.Gte(lower)
            & utf8.Lte(upper);

        return utf8 | changeCase;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector128<byte> ToLower(Vector128<byte> ascii)
    {
        var lower = Vector128.Create((byte)'A');
        var upper = Vector128.Create((byte)'Z');
        var mask = Vector128.Create((byte)0x20);

        var changeCase = mask
            & ascii.Gte(lower)
            & ascii.Lte(upper);

        return ascii | changeCase;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector64<byte> ToLower(Vector64<byte> ascii)
    {
        var lower = Vector64.Create((byte)'A');
        var upper = Vector64.Create((byte)'Z');
        var mask = Vector64.Create((byte)0x20);

        var changeCase = mask
            & Vector64.GreaterThanOrEqual(ascii, lower)
            & Vector64.LessThanOrEqual(ascii, upper);

        return ascii | changeCase;
    }

    internal static void ToLowerCore(ref byte src, ref byte dst, nuint length)
    {
        nuint offset = 0;
        if (length >= (nuint)Vector256<byte>.Count)
        {
            var lower = Vector256.Create((byte)'A');
            var upper = Vector256.Create((byte)'Z');
            var mask = Vector256.Create((byte)0x20);

            var lastvec = length - (nuint)Vector256<byte>.Count;
            do
            {
                // We cannot use extension helper because the compiler refuses
                // to hoist constant loads out of the loop on ARM64
                var utf8 = Vector256.LoadUnsafe(ref src.Add(offset));

                var changeCase = mask
                    & utf8.Gte(lower)
                    & utf8.Lte(upper);

                (utf8 | changeCase).StoreUnsafe(ref dst.Add(offset));

                offset += (nuint)Vector256<byte>.Count;
            } while (offset <= lastvec);
        }

        if (offset <= length - (nuint)Vector128<byte>.Count)
        {
            var utf8 = Vector128.LoadUnsafe(ref src, offset);

            Instance
                .ToLower(utf8)
                .StoreUnsafe(ref dst.Add(offset));

            offset += (nuint)Vector128<byte>.Count;
        }

        if (Vector64.IsHardwareAccelerated &&
            offset <= length - (nuint)Vector64<byte>.Count)
        {
            var utf8 = Vector64.LoadUnsafe(ref src, offset);

            Instance
                .ToLower(utf8)
                .StoreUnsafe(ref dst.Add(offset));

            offset += (nuint)Vector64<byte>.Count;
        }

        while (offset < length)
        {
            var b = src.Add(offset);
            if (b is >= (byte)'A' and <= (byte)'Z')
            {
                b |= 0x20;
            }

            dst.Add(offset) = b;
            offset++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ToUpper(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        if (destination.Length < source.Length)
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        ToUpperCore(ref source.AsRef(), ref destination.AsRef(), (nuint)source.Length);
        return source.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector256<byte> ToUpper(Vector256<byte> utf8)
    {
        var lower = Vector256.Create((byte)'a');
        var upper = Vector256.Create((byte)'z');
        var mask = Vector256.Create((byte)0x20);

        var changeCase = mask
            & utf8.Gte(lower)
            & utf8.Lte(upper);

        return utf8 ^ changeCase;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector128<byte> ToUpper(Vector128<byte> ascii)
    {
        var lower = Vector128.Create((byte)'a');
        var upper = Vector128.Create((byte)'z');
        var mask = Vector128.Create((byte)0x20);

        var changeCase = mask
            & ascii.Gte(lower)
            & ascii.Lte(upper);

        return ascii ^ changeCase;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector64<byte> ToUpper(Vector64<byte> ascii)
    {
        var lower = Vector64.Create((byte)'a');
        var upper = Vector64.Create((byte)'z');
        var mask = Vector64.Create((byte)0x20);

        var changeCase = mask
            & Vector64.GreaterThanOrEqual(ascii, lower)
            & Vector64.LessThanOrEqual(ascii, upper);

        return ascii ^ changeCase;
    }

    internal static void ToUpperCore(ref byte src, ref byte dst, nuint length)
    {
        nuint offset = 0;
        if (length >= (nuint)Vector256<byte>.Count)
        {
            // As usual, .NET unrolls this into 128x2 when 256 is not available
            var lower = Vector256.Create((byte)'a');
            var upper = Vector256.Create((byte)'z');
            var mask = Vector256.Create((byte)0x20);

            var lastvec = length - (nuint)Vector256<byte>.Count;
            do
            {
                var utf8 = Vector256.LoadUnsafe(ref src.Add(offset));

                var changeCase = mask
                    & utf8.Gte(lower)
                    & utf8.Lte(upper);

                (utf8 ^ changeCase).StoreUnsafe(ref dst.Add(offset));

                offset += (nuint)Vector256<byte>.Count;
            } while (offset <= lastvec);
        }

        if (offset <= length - (nuint)Vector128<byte>.Count)
        {
            var utf8 = Vector128.LoadUnsafe(ref src, offset);

            Instance
                .ToUpper(utf8)
                .StoreUnsafe(ref dst.Add(offset));

            offset += (nuint)Vector128<byte>.Count;
        }

        if (Vector64.IsHardwareAccelerated &&
            offset <= length - (nuint)Vector64<byte>.Count)
        {
            var utf8 = Vector64.LoadUnsafe(ref src, offset);

            Instance
                .ToUpper(utf8)
                .StoreUnsafe(ref dst.Add(offset));

            offset += (nuint)Vector64<byte>.Count;
        }

        // TODO: Pack the last few bytes and do bitwise case change
        // Especially important if this is V256/512
        while (offset < length)
        {
            var b = src.Add(offset);
            if (b is >= (byte)'a' and <= (byte)'z')
            {
                b ^= 0x20;
            }

            dst.Add(offset) = b;
            offset++;
        }
    }
}
