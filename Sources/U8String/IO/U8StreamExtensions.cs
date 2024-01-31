namespace U8.IO;

public static class U8StreamExtensions
{
    public static U8Reader<U8StreamSource> AsU8Reader(this Stream stream)
    {
        ThrowHelpers.CheckNull(stream);

        return new(new(stream));
    }

    public static U8LineReader<U8StreamSource> ReadU8Lines(this Stream stream)
    {
        ThrowHelpers.CheckNull(stream);

        return new(new(new(stream)));
    }
}
