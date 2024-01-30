namespace U8.IO;

public static class U8StreamExtensions
{
    public static U8LineReader<U8StreamSource> ReadLines(this Stream stream)
    {
        ThrowHelpers.CheckNull(stream);

        return new(new(new(stream)));
    }

    public static U8AsyncLineReader<U8StreamSource> ReadLinesAsync(
        this Stream stream, CancellationToken ct = default)
    {
        ThrowHelpers.CheckNull(stream);

        return new(new(new(stream)), ct);
    }
}
