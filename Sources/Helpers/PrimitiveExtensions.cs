using System.Text;

namespace U8.Primitives;

static class PrimitiveExtensions
{
    internal static int TotalLength(this U8Range[] ranges)
    {
        var total = 0;
        foreach (var range in ranges)
        {
            total += range.Length;
        }

        return total;
    }

    internal static U8TwoBytes AsTwoBytes(this char c) => new(c);
    internal static U8TwoBytes AsTwoBytes(this Rune r) => new(r);
    internal static U8ThreeBytes AsThreeBytes(this char c) => new(c);
    internal static U8ThreeBytes AsThreeBytes(this Rune r) => new(r);
    internal static U8FourBytes AsFourBytes(this Rune r) => new(r);
}
