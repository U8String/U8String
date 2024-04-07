using Microsoft.Win32.SafeHandles;

using U8.Primitives;

namespace U8.IO;

public static class U8File
{
    public static U8Reader<U8FileSource> OpenRead(string path)
    {
        var handle = File.OpenHandle(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            OperatingSystem.IsWindows() ? FileOptions.None : FileOptions.Asynchronous);

        return new(new(handle), disposeSource: true);
    }

    /// <inheritdoc cref="U8String.Read(SafeFileHandle, long)"/>
    public static U8String Read(
        string path,
        long offset = 0,
        int length = -1,
        bool stripBOM = false,
        bool roundTrailingRune = false)
    {
        ThrowHelpers.CheckNull(path);

        using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Read(handle, offset, length, stripBOM, roundTrailingRune);
    }

    public static U8String Read(
        SafeFileHandle handle,
        long offset = 0,
        int length = -1,
        bool stripBOM = false,
        bool roundTrailingRune = false)
    {
        if (length is -1)
        {
            var fileLength = RandomAccess.GetLength(handle);
            if (fileLength < 0 || fileLength > U8String.MaxSafeLength)
            {
                ThrowHelpers.ArgumentException("The file or file segment is too large to be read into a U8String.");
            }

            length = (int)fileLength;
        }

        var nullTerminate = length < U8String.MaxSafeLength;
        var buffer = new byte[nullTerminate ? length + 1 : length];

        length = RandomAccess.Read(handle, buffer.AsSpan(0, length), offset);

        var range = roundTrailingRune
            ? RoundTrailingRune(buffer.AsSpan(0, length))
            : new(0, length);

        var result = new U8String(buffer, range);
        U8String.Validate(result);
        if (stripBOM)
        {
            result = result.StripPrefix(U8Constants.ByteOrderMark);
        }

        return result;
    }

    public static async Task<U8String> ReadAsync(
        string path,
        long offset = 0,
        int length = -1,
        bool stripBOM = false,
        bool roundTrailingRune = false,
        CancellationToken ct = default)
    {
        ThrowHelpers.CheckNull(path);

        using var handle = File.OpenHandle(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            OperatingSystem.IsWindows() ? FileOptions.None : FileOptions.Asynchronous);

        return await ReadAsync(handle, offset, length, stripBOM, roundTrailingRune, ct);
    }

    public static async Task<U8String> ReadAsync(
        SafeFileHandle handle,
        long offset = 0,
        int length = -1,
        bool stripBOM = false,
        bool roundTrailingRune = false,
        CancellationToken ct = default)
    {
        if (length is -1)
        {
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
            var fileLength = RandomAccess.GetLength(handle);
            if (fileLength < 0 || fileLength > U8String.MaxSafeLength)
            {
                ThrowHelpers.ArgumentException("The file or file segment is too large to be read into a U8String.");
            }

            length = (int)fileLength;
        }

        var nullTerminate = length < U8String.MaxSafeLength;
        var buffer = new byte[nullTerminate ? length + 1 : length];

        length = await RandomAccess
            .ReadAsync(handle, buffer.AsMemory(0, length), offset, ct)
            .ConfigureAwait(false);

        var range = roundTrailingRune
            ? RoundTrailingRune(buffer.AsSpan(0, length))
            : new(0, length);

        var result = new U8String(buffer, range);
        U8String.Validate(result);
        if (stripBOM)
        {
            result = result.StripPrefix(U8Constants.ByteOrderMark);
        }

        return result;
    }

    public static U8LineReader<U8FileSource> ReadLines(string path)
    {
        var handle = File.OpenHandle(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            OperatingSystem.IsWindows() ? FileOptions.None : FileOptions.Asynchronous);

        return new(new(new(handle), disposeSource: true), disposeReader: true);
    }

    static U8Range RoundTrailingRune(ReadOnlySpan<byte> buffer)
    {
        ref var ptr = ref buffer.AsRef();

        var b = (byte)0;
        var trimmed = 0;
        var length = buffer.Length;
        while (length > 0 && trimmed < 4)
        {
            b = ptr.Add(length - trimmed - 1);

            if (U8Info.IsBoundaryByte(b))
            {
                break;
            }

            length--;
            trimmed++;
        }

        var runeLength = U8Info.RuneLength(b);
        if (runeLength == trimmed)
        {
            length += trimmed;
        }
        else if (runeLength < trimmed)
        {
            ThrowHelpers.InvalidUtf8();
        }
        
        return new(0, length);
    }
}
