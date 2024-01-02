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
}
