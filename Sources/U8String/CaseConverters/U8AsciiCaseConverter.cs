using System.Runtime.Intrinsics;

using U8.Abstractions;

namespace U8.CaseConversion;

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
            ThrowHelpers.DestinationTooShort();
        }

        ToLowerCore(ref source.AsRef(), ref destination.AsRef(), (uint)source.Length);
        return source.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector256<byte> ToLower(Vector256<byte> utf8)
    {
        var mask = Vector256.Create((sbyte)0x20);
        var overflow = Vector256.Create<sbyte>(128 - 'A');
        var bound = Vector256.Create<sbyte>(-127 + ('Z' - 'A'));

        return utf8 | ((utf8.AsSByte() + overflow).Lt(bound) & mask).AsByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector128<byte> ToLower(Vector128<byte> utf8)
    {
        var mask = Vector128.Create((sbyte)0x20);
        var overflow = Vector128.Create<sbyte>(128 - 'A');
        var bound = Vector128.Create<sbyte>(-127 + ('Z' - 'A'));

        return utf8 | ((utf8.AsSByte() + overflow).Lt(bound) & mask).AsByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector64<byte> ToLower(Vector64<byte> utf8)
    {
        var mask = Vector64.Create((sbyte)0x20);
        var overflow = Vector64.Create<sbyte>(128 - 'A');
        var bound = Vector64.Create<sbyte>(-127 + ('Z' - 'A'));

        return utf8 | (Vector64.LessThan(utf8.AsSByte() + overflow, bound) & mask).AsByte();
    }

    internal static void ToLowerCore(ref byte src, ref byte dst, nuint length)
    {
        if (!BitConverter.IsLittleEndian)
        {
            ThrowHelpers.NotSupportedBigEndian();
        }

        nuint offset = 0;
        if (length >= (nuint)Vector256<byte>.Count)
        {
            var mask = Vector256.Create((sbyte)0x20);
            var overflow = Vector256.Create<sbyte>(128 - 'A');
            var bound = Vector256.Create<sbyte>(-127 + ('Z' - 'A'));

            var lastvec = length - (nuint)Vector256<byte>.Count;
            do
            {
                var utf8 = Vector256.LoadUnsafe(ref src, offset).AsSByte();
                var changeCase = (utf8 + overflow).Lt(bound) & mask;

                (utf8 | changeCase).AsByte().StoreUnsafe(ref dst, offset);

                offset += (nuint)Vector256<byte>.Count;
            } while (offset <= lastvec);
        }

        if (length >= offset + (nuint)Vector128<byte>.Count)
        {
            var utf8 = Vector128.LoadUnsafe(ref src, offset);
            ToLower(utf8).StoreUnsafe(ref dst, offset);
            offset += (nuint)Vector128<byte>.Count;
        }

        if (Vector64.IsHardwareAccelerated &&
            length >= offset + (nuint)Vector64<byte>.Count)
        {
            var utf8 = Vector64.LoadUnsafe(ref src, offset);
            ToLower(utf8).StoreUnsafe(ref dst, offset);
            offset += (nuint)Vector64<byte>.Count;
        }

        while (offset < length)
        {
            var b = src.Add(offset);
            if (U8Info.IsAsciiLetter(b))
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
            ThrowHelpers.DestinationTooShort();
        }

        ToUpperCore(ref source.AsRef(), ref destination.AsRef(), (uint)source.Length);
        return source.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector256<byte> ToUpper(Vector256<byte> utf)
    {
        var mask = Vector256.Create((sbyte)0x20);
        var overflow = Vector256.Create<sbyte>(128 - 'a');
        var bound = Vector256.Create<sbyte>(-127 + ('z' - 'a'));

        return utf ^ ((utf.AsSByte() + overflow).Lt(bound) & mask).AsByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector128<byte> ToUpper(Vector128<byte> utf)
    {
        var mask = Vector128.Create((sbyte)0x20);
        var overflow = Vector128.Create<sbyte>(128 - 'a');
        var bound = Vector128.Create<sbyte>(-127 + ('z' - 'a'));

        return utf ^ ((utf.AsSByte() + overflow).Lt(bound) & mask).AsByte();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector64<byte> ToUpper(Vector64<byte> ascii)
    {
        var mask = Vector64.Create((sbyte)0x20);
        var overflow = Vector64.Create<sbyte>(128 - 'a');
        var bound = Vector64.Create<sbyte>(-127 + ('z' - 'a'));

        return ascii ^ (Vector64.LessThan(ascii.AsSByte() + overflow, bound) & mask).AsByte();
    }

    internal static void ToUpperCore(ref byte src, ref byte dst, nuint length)
    {
        if (!BitConverter.IsLittleEndian)
        {
            ThrowHelpers.NotSupportedBigEndian();
        }

        nuint offset = 0;
        if (length >= (nuint)Vector256<byte>.Count)
        {
            // As usual, .NET unrolls this into 128x2 when 256 is not available
            var mask = Vector256.Create((sbyte)0x20);
            var overflow = Vector256.Create<sbyte>(128 - 'a');
            var bound = Vector256.Create<sbyte>(-127 + ('z' - 'a'));

            var lastvec = length - (nuint)Vector256<byte>.Count;
            do
            {
                var utf8 = Vector256.LoadUnsafe(ref src, offset).AsSByte();
                var changeCase = (utf8 + overflow).Lt(bound) & mask;

                (utf8 ^ changeCase).AsByte().StoreUnsafe(ref dst, offset);

                offset += (nuint)Vector256<byte>.Count;
            } while (offset <= lastvec);
        }

        if (length >= offset + (nuint)Vector128<byte>.Count)
        {
            var utf8 = Vector128.LoadUnsafe(ref src, offset);
            ToUpper(utf8).StoreUnsafe(ref dst, offset);
            offset += (nuint)Vector128<byte>.Count;
        }

        if (Vector64.IsHardwareAccelerated &&
            length >= offset + (nuint)Vector64<byte>.Count)
        {
            var utf8 = Vector64.LoadUnsafe(ref src, offset);
            ToUpper(utf8).StoreUnsafe(ref dst, offset);
            offset += (nuint)Vector64<byte>.Count;
        }

        // TODO: Pack the last few bytes and do bitwise case change
        // Especially important if this is V256/512
        while (offset < length)
        {
            var b = src.Add(offset);
            if (U8Info.IsAsciiLetter(b))
            {
                b &= unchecked((byte)~0x20);
            }

            dst.Add(offset) = b;
            offset++;
        }
    }
}
