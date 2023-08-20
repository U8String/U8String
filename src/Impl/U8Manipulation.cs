using System.Runtime.Intrinsics;
using System.Text;

namespace U8Primitives;

internal static class U8Manipulation
{
    internal static U8String ConcatUnchecked(ReadOnlySpan<byte> left, byte right)
    {
        var length = left.Length + 1;
        var value = new byte[length];

        left.CopyTo(value);
        value[length - 1] = right;

        return new U8String(value, 0, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static U8String ConcatUnchecked(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        var length = left.Length + right.Length;
        var value = new byte[length];

        left.CopyTo(value);
        right.CopyTo(value.SliceUnsafe(left.Length));

        return new U8String(value, 0, length);
    }

    internal static U8String Replace(U8String source, byte oldValue, byte newValue)
    {
        if (!source.IsEmpty)
        {
            var current = source.UnsafeSpan;
            var firstReplace = current.IndexOf(oldValue);
            if (firstReplace < 0)
            {
                return source;
            }

            var replaced = new byte[source.Length];
            var destination = replaced.AsSpan();

            current
                .SliceUnsafe(0, firstReplace)
                .CopyTo(destination);

            destination = destination.SliceUnsafe(firstReplace);
            current
                .SliceUnsafe(firstReplace)
                .Replace(destination, oldValue, newValue);

            // Old and new bytes which individually are invalid unicode scalar values
            // are allowed if the replacement produces a valid UTF-8 sequence.
            U8String.Validate(destination);
            return new(replaced, 0, source.Length);
        }

        return default;
    }

    internal static U8String Replace(U8String source, char oldValue, char newValue)
    {
        return char.IsAscii(oldValue) && char.IsAscii(newValue)
            ? Replace(source, (byte)oldValue, (byte)newValue)
            : ReplaceUnchecked(source, oldValue.NonAsciiToUtf8(out _), newValue.NonAsciiToUtf8(out _));
    }

    internal static U8String Replace(U8String source, Rune oldValue, Rune newValue)
    {
        return oldValue.IsAscii && newValue.IsAscii
            ? Replace(source, (byte)oldValue.Value, (byte)newValue.Value)
            : ReplaceUnchecked(source, oldValue.NonAsciiToUtf8(out _), newValue.NonAsciiToUtf8(out _));
    }

    // TODO: Input args contract - throw on empty old value?
    internal static U8String Replace(U8String source, ReadOnlySpan<byte> oldValue, ReadOnlySpan<byte> newValue)
    {
        if (!source.IsEmpty)
        {
            if (oldValue.Length is 1 && newValue.Length is 1)
            {
                return Replace(source, oldValue[0], newValue[0]);
            }

            throw new NotImplementedException();
        }

        return default;
    }

    internal static U8String ReplaceUnchecked(U8String source, byte oldValue, byte newValue)
    {
        throw new NotImplementedException();
    }

    internal static U8String ReplaceUnchecked(U8String source, ReadOnlySpan<byte> oldValue, ReadOnlySpan<byte> newValue)
    {
        throw new NotImplementedException();
    }

    internal static void ToUpperAscii(ref byte src, ref byte dst, nuint length)
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
                var chunk = Vector256.LoadUnsafe(ref src.Add(offset));
                var isLower = Vector256.GreaterThanOrEqual(chunk, lower);
                var isUpper = Vector256.LessThanOrEqual(chunk, upper);
                var isLetter = isLower & isUpper;
                var changeCase = isLetter & mask;

                chunk ^= changeCase;
                chunk.StoreUnsafe(ref dst.Add(offset));
                offset += (nuint)Vector256<byte>.Count;
            } while (offset <= lastvec);
        }

        if (Vector64.IsHardwareAccelerated && length >= (nuint)Vector64<byte>.Count)
        {
            var lower = Vector64.Create((byte)'a');
            var upper = Vector64.Create((byte)'z');
            var mask = Vector64.Create((byte)0x20);

            var lastvec = length - (nuint)Vector64<byte>.Count;
            do
            {
                var chunk = Vector64.LoadUnsafe(ref src.Add(offset));
                var isLower = Vector64.GreaterThanOrEqual(chunk, lower);
                var isUpper = Vector64.LessThanOrEqual(chunk, upper);
                var isLetter = isLower & isUpper;
                var changeCase = isLetter & mask;

                chunk ^= changeCase;
                chunk.StoreUnsafe(ref dst.Add(offset));
                offset += (nuint)Vector64<byte>.Count;
            } while (offset <= lastvec);
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

    internal static void ToLowerAscii(ref byte src, ref byte dst, nuint length)
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
                var chunk = Vector256.LoadUnsafe(ref src.Add(offset));
                var isLower = Vector256.GreaterThanOrEqual(chunk, lower);
                var isUpper = Vector256.LessThanOrEqual(chunk, upper);
                var isLetter = isLower & isUpper;
                var changeCase = isLetter & mask;

                chunk |= changeCase;
                chunk.StoreUnsafe(ref dst.Add(offset));
                offset += (nuint)Vector256<byte>.Count;
            } while (offset <= lastvec);
        }

        if (Vector64.IsHardwareAccelerated && length >= (nuint)Vector64<byte>.Count)
        {
            var lower = Vector64.Create((byte)'A');
            var upper = Vector64.Create((byte)'Z');
            var mask = Vector64.Create((byte)0x20);

            var lastvec = length - (nuint)Vector64<byte>.Count;
            do
            {
                var chunk = Vector64.LoadUnsafe(ref src.Add(offset));
                var isLower = Vector64.GreaterThanOrEqual(chunk, lower);
                var isUpper = Vector64.LessThanOrEqual(chunk, upper);
                var isLetter = isLower & isUpper;
                var changeCase = isLetter & mask;

                chunk |= changeCase;
                chunk.StoreUnsafe(ref dst.Add(offset));
                offset += (nuint)Vector64<byte>.Count;
            } while (offset <= lastvec);
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
}
