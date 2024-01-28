namespace U8.IO;

public static class U8File
{
    public static U8LineReader<U8ReaderSource.File> ReadLines(string path)
    {
        var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read);

        return new(new(new(handle)));
    }

    public static U8AsyncLineReader<U8ReaderSource.File> ReadLinesAsync(
        string path, CancellationToken ct = default)
    {
        var handle = File.OpenHandle(
            path, FileMode.Open, FileAccess.Read, FileShare.Read);

        return new(new(new(handle)), ct);
    }

    public static async IAsyncEnumerable<U8String> ReadLinesAsync2(
        string path, [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var handle = File.OpenHandle(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.Asynchronous);

        var reader = new U8Reader<U8ReaderSource.File>(new(handle));
        while (await reader.ReadToAsync((byte)'\n', ct) is U8String line)
        {
            yield return line.StripSuffix((byte)'\r');
        }
    }
}
