using System.Buffers;
using U8Primitives.Abstractions;

namespace U8Primitives;

// TODO: Naming?
public static class U8CaseConversion
{
    public static AsciiConverter Ascii => default;

    public static FallbackInvariantConverter FallbackInvariant => default;

    public readonly struct AsciiConverter : IU8CaseConverter
    {
        public static AsciiConverter Instance => default;

        public (int ReplaceStart, int LowercaseLength) LowercaseHint(ReadOnlySpan<byte> source)
        {
            var firstByte = source.IndexOfAnyInRange((byte)'A', (byte)'Z');

            return firstByte >= 0
                ? (firstByte, source.Length)
                : (source.Length, source.Length);
        }

        public (int ReplaceStart, int UppercaseLength) UppercaseHint(ReadOnlySpan<byte> source)
        {
            var firstByte = source.IndexOfAnyInRange((byte)'a', (byte)'z');
            
            return firstByte >= 0
                ? (firstByte, source.Length)
                : (source.Length, source.Length);
        }

        public int ToLower(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            if (destination.Length < source.Length)
            {
                ThrowHelpers.ArgumentOutOfRange();
            }

            U8Manipulation.ToLowerAscii(
                ref source.AsRef(),
                ref destination.AsRef(),
                (nuint)source.Length);

            return source.Length;
        }

        public int ToUpper(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            if (destination.Length < source.Length)
            {
                ThrowHelpers.ArgumentOutOfRange();
            }

            U8Manipulation.ToUpperAscii(
                ref source.AsRef(),
                ref destination.AsRef(),
                (nuint)source.Length);

            return source.Length;
        }
    }

    public readonly struct FallbackInvariantConverter : IU8CaseConverter
    {
        public static FallbackInvariantConverter Instance => default;

        public (int ReplaceStart, int LowercaseLength) LowercaseHint(ReadOnlySpan<byte> source)
        {
            throw new NotImplementedException();
        }

        public (int ReplaceStart, int UppercaseLength) UppercaseHint(ReadOnlySpan<byte> source)
        {
            throw new NotImplementedException();
        }

        public int ToLower(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            if (destination.Length < source.Length)
            {
                ThrowHelpers.ArgumentOutOfRange();
            }

            ref var dst = ref destination.AsRef();

            var result = System.Text.Ascii.ToLower(source, destination, out var consumed);
            if (result is OperationStatus.InvalidData)
            {
                // foreach (var rune in U8Marshal.Slice(source, consumed).Runes)
                // {
                //     var lower = Rune.ToLowerInvariant(rune);
                //     var scalar = U8Scalar.Create(lower);
                //     if (consumed + 4 > destination.Length)
                //     {
                //         [DoesNotReturn]
                //         static void Unimpl()
                //         {
                //             throw new NotImplementedException();
                //         }

                //         Unimpl();
                //     }

                //     scalar.StoreUnsafe(ref dst.Add(consumed));
                //     consumed += scalar.Size;
                // }
                throw new NotImplementedException();
            }

            return consumed;
        }

        public int ToUpper(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            if (destination.Length < source.Length)
            {
                ThrowHelpers.ArgumentOutOfRange();
            }

            ref var dst = ref destination.AsRef();

            var result = System.Text.Ascii.ToUpper(source, destination, out var consumed);
            if (result is OperationStatus.InvalidData)
            {
                // foreach (var rune in U8Marshal.Slice(source, consumed).Runes)
                // {
                //     var upper = Rune.ToUpperInvariant(rune);
                //     var scalar = U8Scalar.Create(upper);
                //     if (consumed + 4 > destination.Length)
                //     {
                //         [DoesNotReturn]
                //         static void Unimpl()
                //         {
                //             throw new NotImplementedException();
                //         }

                //         Unimpl();
                //     }

                //     scalar.StoreUnsafe(ref dst.Add(consumed));
                //     consumed += scalar.Size;
                // }
                throw new NotImplementedException();
            }

            return consumed;
        }
    }
}
