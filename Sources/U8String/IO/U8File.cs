using Microsoft.Win32.SafeHandles;

using U8.Primitives;

namespace U8.IO;

public static class U8File
{
    public static U8Reader<U8FileSource> OpenRead(string path)
    {
        var handle = File.OpenHandle(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.Asynchronous);

        return new(new(handle));
    }

    /// <inheritdoc cref="U8String.Read(SafeFileHandle, long)"/>
    public static U8String Read(string path, long offset = 0, int length = -1, bool roundOffsets = false)
    {
        ThrowHelpers.CheckNull(path);

        using var handle = File.OpenHandle(path);

        // TODO: Handle files that are unseekable and/or of -1 length
        if (length is -1)
            length = int.CreateChecked(RandomAccess.GetLength(handle));

        var start = 0;
        var buffer = new byte[(nint)(uint)length + 1];

        length = RandomAccess.Read(handle, buffer, offset);

        if (offset is 0 && HasBOM(buffer.SliceUnsafe(0, length)))
        {
            start = 3;
            length -= 3;
        }

        var range = roundOffsets
            ? RoundOffsets(buffer, start, length)
            : new(start, length);

        var result = new U8String(buffer, range);
        U8String.Validate(result);
        return result;
    }

    public static U8LineReader<U8FileSource> ReadLines(string path)
    {
        var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read);

        return new(new(new(handle)));
    }

    public static async IAsyncEnumerable<U8String> ReadLines2(
        string path, [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var handle = File.OpenHandle(
            path, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.Asynchronous);

        var reader = new U8Reader<U8FileSource>(new(handle));
        while (await reader.ReadToAsync((byte)'\n', ct) is U8String line)
        {
            yield return line.StripSuffix((byte)'\r');
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool HasBOM(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length >= 3)
        {
            ref var ptr = ref buffer.AsRef();

            var b01 = ptr.Cast<byte, ushort>();
            var b2 = ptr.Add(2);

            return b01 is (0xEF | (0xBB << 8)) && b2 is 0xBF;
        }

        return false;
    }

    static U8Range RoundOffsets(byte[] buffer, int start, int length)
    {
        ref var ptr = ref buffer.AsRef(start);

        var searchStart = 0;
        while (start < length
            && U8Info.IsContinuationByte(in ptr.Add(searchStart)))
        {
            searchStart++;
        }

        start += searchStart;
        length -= searchStart;

        if (length > 0)
        {
            var searchEnd = length;
            while (searchEnd > 0
                && U8Info.IsContinuationByte(in ptr.Add(searchEnd)))
            {
                searchEnd--;
            }

            var slicedOff = length - searchEnd;
            var lastRuneLength = U8Info.RuneLength(in ptr.Add(searchEnd));

            // If the buffer that we read doesn't include the full rune at the end - slice it away
            // completely, there is no way to know whether the end result would be valid UTF-8 and
            // since the caller asked us to round the offsets, we know that omitting a portion of the data is acceptable.
            if (lastRuneLength > slicedOff)
            {
                // Done slicing, should the next rune at the end too be invalid, we do want to throw
                // instead since the rest of the data is subject to validation.
                length = searchEnd - 1;
            }
            else
            {
                length = searchEnd + lastRuneLength;
            }
        }

        return new(start, length);
    }
}
