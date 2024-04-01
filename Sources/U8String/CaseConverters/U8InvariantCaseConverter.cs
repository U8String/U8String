using System.Buffers;
using System.Text;

using U8.Abstractions;
using U8.Extensions;

namespace U8.CaseConversion;

public readonly struct U8InvariantCaseConverter : IU8CaseConverter
{
    // If you suffer from OCD, I would like to apologize in advance.
    static readonly SearchValues<byte> LowerFilter = SearchValues.Create(
    [
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
        20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37,
        38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55,
        56, 57, 58, 59, 60, 61, 62, 63, 64, 91, 92, 93, 94, 95, 96, 97, 98, 99,
        100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 112, 113,
        114, 115, 116, 117, 118, 119, 120, 121, 122,  123, 124, 125, 126, 127
    ]);

    static readonly SearchValues<byte> UpperFilter = SearchValues.Create(
    [
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
        20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37,
        38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55,
        56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73,
        74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91,
        92, 93, 94, 95, 96, 123, 124, 125, 126, 127
    ]);

    public bool IsFixedLength => false;

    public int FindToLowerStart(ReadOnlySpan<byte> source)
    {
        return source.IndexOfAnyExcept(LowerFilter);
    }

    public int FindToUpperStart(ReadOnlySpan<byte> source)
    {
        return source.IndexOfAnyExcept(UpperFilter);
    }

    public int ToLower(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        throw new NotSupportedException();
    }

    public void ToLower(ReadOnlySpan<byte> source, ref InlineU8Builder destination)
    {
        destination.EnsureCapacity(source.Length);
        Ascii.ToLower(source, destination.Free, out var written);

        destination.BytesWritten += written;
        source = source.Slice(written);

        if (source.Length > 0)
        {
            ToLowerCore(source, ref destination);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ToLowerCore(ReadOnlySpan<byte> source, ref InlineU8Builder destination)
        {
            // This has absolutely terrible performance but it's better than nothing.
            foreach (var rune in source.EnumerateRunes())
            {
                // TODO: Pre-size the buffer to source.Length * 3 or Array.MaxLength, whichever is smaller;
                // and then do bounds checked appends but with skipped growth checks and source selection.
                // Switch on rune length too and perform unrolled copy.
                destination.AppendFormatted(Rune.ToLowerInvariant(rune));
            }
        }
    }

    public int ToUpper(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        throw new NotSupportedException();
    }

    public void ToUpper(ReadOnlySpan<byte> source, ref InlineU8Builder destination)
    {
        destination.EnsureCapacity(source.Length);
        Ascii.ToUpper(source, destination.Free, out var written);

        destination.BytesWritten += written;
        source = source.Slice(written);
        
        if (source.Length > 0)
        {
            ToUpperCore(source, ref destination);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void ToUpperCore(ReadOnlySpan<byte> source, ref InlineU8Builder destination)
        {
            foreach (var rune in source.EnumerateRunes())
            {
                destination.AppendFormatted(Rune.ToUpperInvariant(rune));
            }
        }
    }
}
