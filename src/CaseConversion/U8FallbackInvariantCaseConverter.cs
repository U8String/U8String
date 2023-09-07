using System.Buffers;

using U8Primitives.Abstractions;

namespace U8Primitives;

// TODO: This is a long type name. Consider renaming?
public readonly struct U8FallbackInvariantCaseConverter : IU8CaseConverter
{
    public static U8FallbackInvariantCaseConverter Instance => default;

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