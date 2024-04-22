using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Win32.SafeHandles;

namespace U8.IO;

// TODO: Make offset + length separate overloads
public static class U8File
{
    public static U8Reader<U8FileSource> OpenRead(string path)
    {
        // Similar to File.ReadAllBytes, .SequentialScan requires
        // an extra syscall on Unix, which does not seem to be worth it.
        // However, on Windows, unlike .ReadAllBytes, we avoid going
        // through OverlappedIO which turns out has severe overhead
        // to the point where it is better to impose certain degree
        // of starvation on ThreadPool and perform reads synchronously.
        // Do note that currently .Asynchronous does nothing on Unix,
        // but hopefully .NET's RandomAccess will switch to epoll/io_uring
        // eventually for IOPS-optimized file access.
        var options = OperatingSystem.IsWindows()
            ? FileOptions.SequentialScan
            : FileOptions.Asynchronous;

        var handle = File.OpenHandle(path, options: options);

        return new(new(handle), disposeSource: true);
    }

    public static U8String Read(string path) =>
        Read(path, 0, -1, false, false);

    public static U8String Read(SafeFileHandle handle) =>
        Read(handle, 0, -1, false, false);

    public static U8String Read(
        string path,
        long offset,
        int length = -1,
        bool stripBOM = false,
        bool roundTrailingRune = false)
    {
        ArgumentNullException.ThrowIfNull(path);

        var options = OperatingSystem.IsWindows()
            ? FileOptions.SequentialScan
            : FileOptions.None;

        using var handle = File.OpenHandle(path, options: options);
        return Read(handle, offset, length, stripBOM, roundTrailingRune);
    }

    public static U8String Read(
        SafeFileHandle handle,
        long offset,
        int length = -1,
        bool stripBOM = false,
        bool roundTrailingRune = false)
    {
        if (offset < 0)
            ThrowHelpers.ArgumentException("The offset must be non-negative.");
        if (length < -1 || length > U8String.MaxSafeLength)
            ThrowHelpers.ArgumentException("The length must between -1 and U8String.MaxSafeLength inclusive.");

        var readLength = GetReadLength(handle, offset, length);
        if (readLength != 0)
        {

            var result = readLength > 0
                ? ReadSized(handle, offset, readLength)
                : ReadUnsized(handle);

            if (stripBOM)
                result = result.StripPrefix(U8Constants.ByteOrderMark);

            if (roundTrailingRune)
                result = RoundTrailingRune(result);


            U8String.Validate(result);
            return result;
        }

        return ReadZeroLength(handle, offset);

        static U8String ReadSized(SafeFileHandle handle, long offset, int length)
        {
            Debug.Assert(offset >= 0);
            Debug.Assert(length > 0);
            Debug.Assert(length <= U8String.MaxSafeLength);

            var buffer = new byte[length + 1];

            var index = 0;
            var count = length;
            while (count > 0)
            {
                var n = RandomAccess.Read(handle, buffer.AsSpan(index, count), offset + index);
                if (n is 0) EndOfFile();

                index += n;
                count -= n;
            }

            return new(buffer, length, neverEmpty: true);
        }

        static U8String ReadUnsized(SafeFileHandle handle)
        {
            int read;
            var buffer = new PooledU8Builder(8192);

            while ((read = RandomAccess.Read(handle, buffer.Free, 0)) > 0)
            {
                buffer.BytesWritten += read;
                if (buffer.Free.Length <= 0)
                {
                    buffer.Grow();
                }
            }

            var result = new U8String(buffer.Written, skipValidation: true);
            buffer.Dispose();
            return result;
        }

        static U8String ReadZeroLength(SafeFileHandle handle, long offset)
        {
            _ = RandomAccess.Read(handle, Span<byte>.Empty, offset);
            return default;
        }
    }

    public static Task<U8String> ReadAsync(string path, CancellationToken ct = default) =>
        ReadAsync(path, 0, -1, false, false, ct);

    public static Task<U8String> ReadAsync(SafeFileHandle handle, CancellationToken ct = default) =>
        ReadAsync(handle, 0, -1, false, false, ct);

    public static async Task<U8String> ReadAsync(
        string path,
        long offset,
        int length = -1,
        bool stripBOM = false,
        bool roundTrailingRune = false,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(path);
        ct.ThrowIfCancellationRequested();

        var options = OperatingSystem.IsWindows()
            ? FileOptions.SequentialScan
            : FileOptions.Asynchronous;

        using var handle = File.OpenHandle(path, options: options);
        return await ReadAsync(handle, offset, length, stripBOM, roundTrailingRune, ct);
    }

    public static async Task<U8String> ReadAsync(
        SafeFileHandle handle,
        long offset,
        int length = -1,
        bool stripBOM = false,
        bool roundTrailingRune = false,
        CancellationToken ct = default)
    {
        if (offset < 0)
            ThrowHelpers.ArgumentException("The offset must be non-negative.");
        if (length < -1 || length > U8String.MaxSafeLength)
            ThrowHelpers.ArgumentException("The length must between -1 and U8String.MaxSafeLength inclusive.");

        var readLength = GetReadLength(handle, offset, length);
        if (readLength != 0)
        {
            var result = await (readLength > 0
                ? ReadSized(handle, offset, readLength, ct)
                : ReadUnsized(handle, ct))
                    .ConfigureAwait(false);

            if (stripBOM)
                result = result.StripPrefix(U8Constants.ByteOrderMark);

            if (roundTrailingRune)
                result = RoundTrailingRune(result);

            U8String.Validate(result);
            return result;
        }

        await ReadZeroLength(handle, offset, ct).ConfigureAwait(false);
        return default;

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        static async ValueTask<U8String> ReadSized(
            SafeFileHandle handle, long offset, int length, CancellationToken ct)
        {
            Debug.Assert(offset >= 0);
            Debug.Assert(length > 0);
            Debug.Assert(length <= U8String.MaxSafeLength);

            var buffer = new byte[length + 1];

            var index = 0;
            var count = length;
            while (count > 0)
            {
                var n = await RandomAccess
                    .ReadAsync(handle, buffer.AsMemory(index, count), offset + index, ct)
                    .ConfigureAwait(false);
                if (n is 0) EndOfFile();

                index += n;
                count -= n;
            }

            return new(buffer, length, neverEmpty: true);
        }

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        static async ValueTask<U8String> ReadUnsized(SafeFileHandle handle, CancellationToken ct)
        {
            int read;
            var buffer = new PooledU8Builder(8192);

            while ((read = await RandomAccess
                .ReadAsync(handle, buffer.FreeMemory, 0, ct)
                .ConfigureAwait(false)) > 0)
            {
                buffer.BytesWritten += read;
                if (buffer.Free.Length <= 0)
                {
                    buffer.Grow();
                }
            }

            var result = new U8String(buffer.Written, skipValidation: true);
            buffer.Dispose();
            return result;
        }

        static ValueTask<int> ReadZeroLength(SafeFileHandle handle, long offset, CancellationToken ct)
        {
            return RandomAccess.ReadAsync(handle, Memory<byte>.Empty, offset, ct);
        }
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

    static int GetReadLength(SafeFileHandle handle, long offset, int length)
    {
        Debug.Assert(offset >= 0);
        Debug.Assert(length >= -1);
        Debug.Assert(length <= U8String.MaxSafeLength);

        long readLength;

        var fileLength = RandomAccess.GetLength(handle);
        // Regular file
        if (fileLength >= 0)
        {
            if (offset > fileLength)
                ThrowHelpers.ArgumentException("The offset is beyond the end of the file.");

            fileLength -= offset;

            readLength = length < 0
                ? fileLength
                : Math.Min(length, fileLength);

            if (readLength > U8String.MaxSafeLength)
                ThrowHelpers.ArgumentException("The file or file segment is too large to be read into a U8String.");
        }
        // Pipe, socket, etc.
        else
        {
            if (offset > 0)
                ThrowHelpers.ArgumentException("The offset must be zero for non-seekable files.");

            readLength = length;
        }

        return (int)readLength;
    }

    static U8String RoundTrailingRune(U8String value)
    {
        var b = (byte)0;
        var trimmed = 1;

        ref var ptr = ref value.DangerousRef;
        while (trimmed <= 4 && trimmed < value.Length)
        {
            b = ptr.Add(value.Length - trimmed);
            if (U8Info.IsBoundaryByte(b))
                break;
            trimmed++;
        }

        var expected = U8Info.RuneLength(b);
        if (expected == trimmed)
            return value;
        if (expected < trimmed)
            ThrowHelpers.InvalidUtf8();

        return value[..^trimmed];
    }

    [DoesNotReturn, StackTraceHidden]
    static void EndOfFile()
    {
        throw new EndOfStreamException("Reached an unexpected end of file.");
    }
}
