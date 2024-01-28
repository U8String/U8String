using System.Collections;
using System.Diagnostics;
using System.Text;

using U8.Abstractions;
using U8.Primitives;
using U8.Shared;

namespace U8.IO;

public enum U8ReadResult
{
    Success = -1,
    EndOfStream = 0,
    InvalidOffset = 1,
    InvalidUtf8 = 2,
}

public interface IU8ReaderSource<T> : IDisposable // TODO: name?
    where T : struct, IU8ReaderSource<T>
{
    int Read(Span<byte> buffer);
    abstract static ValueTask<int> ReadAsync(
        U8Reader<T> reader,
        Memory<byte> buffer,
        CancellationToken ct);
}

public class U8Reader<TSource>(TSource source) : IDisposable
    where TSource : struct, IU8ReaderSource<TSource>
{
    readonly byte[] _buffer = new byte[8192];
    internal TSource Source = source;

    int _bytesRead;
    int _bytesConsumed;

    public U8String? Read(int length, bool roundOffsets = false)
    {
        // var bytes = new byte[(nint)(uint)length + 1];
        // var buffer = bytes.AsSpan();
        // var bytesRead = _stream.Read(buffer);

        throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String? ReadLine()
    {
        var line = ReadTo((byte)'\n');
        if (line.HasValue)
        {
            line = line.Value.StripSuffix((byte)'\r');
        }

        return line;
    }

    public async ValueTask<U8String?> ReadLineAsync(CancellationToken ct = default)
    {
        var line = await ReadToAsync((byte)'\n', ct);
        if (line.HasValue)
        {
            line = line.Value.StripSuffix((byte)'\r');
        }

        return line;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public U8String? ReadTo(byte delimiter)
    {
        var unread = Fill();
        if (unread is [])
        {
            return null;
        }

        // Try to find the delimiter within the first read and
        // copy it directly to the result.
        var index = unread.IndexOf(delimiter);
        if (index >= 0)
        {
            Advance(index + 1);
            return new(unread.SliceUnsafe(0, index));
        }

        var builder = new InterpolatedU8StringHandler(unread.Length);
        Advance(unread.Length);
        builder.AppendBytes(unread);

        while ((unread = Fill()).Length > 0)
        {
            if ((index = unread.IndexOf(delimiter)) >= 0)
            {
                Advance(index + 1);
                builder.AppendBytes(unread.SliceUnsafe(0, index));
                break;
            }

            Advance(unread.Length);
            builder.AppendBytes(unread);
        }

        U8String.Validate(builder.Written);
        return new(ref builder);
    }

    public U8String? ReadTo(char delimiter)
    {
        ThrowHelpers.CheckSurrogate(delimiter);

        return char.IsAscii(delimiter)
            ? ReadTo((byte)delimiter)
            : ReadTo(delimiter <= 0x7ff ? delimiter.AsTwoBytes() : delimiter.AsThreeBytes());
    }

    public U8String? ReadTo(Rune delimiter)
    {
        return delimiter.IsAscii
            ? ReadTo((byte)delimiter.Value)
            : ReadTo(delimiter.Value switch
            {
                <= 0x7ff => delimiter.AsTwoBytes(),
                <= 0xffff => delimiter.AsThreeBytes(),
                _ => delimiter.AsFourBytes()
            });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public U8String? ReadTo(ReadOnlySpan<byte> delimiter)
    {
        if (delimiter.Length <= 0)
        {
            ThrowHelpers.ArgumentException();
        }

        var unread = Fill();
        if (unread is [])
        {
            return null;
        }

        // Try to find the delimiter within the first read and
        // copy it directly to the result.
        var index = unread.IndexOf(delimiter);
        if (index >= 0)
        {
            Advance(index + 1);
            return new(unread.SliceUnsafe(0, index));
        }

        var builder = new InterpolatedU8StringHandler(unread.Length);
        Advance(unread.Length);
        builder.AppendBytes(unread);

        while ((unread = Fill()).Length > 0)
        {
            if ((index = unread.IndexOf(delimiter)) >= 0)
            {
                Advance(index + 1);
                builder.AppendBytes(unread.SliceUnsafe(0, index));
                break;
            }

            Advance(unread.Length);
            builder.AppendBytes(unread);
        }

        U8String.Validate(builder.Written);
        return new(ref builder);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public U8String? ReadToAny(ReadOnlySpan<byte> delimiters)
    {
        if (delimiters.IsEmpty)
        {
            ThrowHelpers.ArgumentException();
        }

        var unread = Fill();
        if (unread is [])
        {
            return null;
        }

        // Try to find the delimiter within the first read and
        // copy it directly to the result.
        var index = unread.IndexOfAny(delimiters);
        if (index >= 0)
        {
            Advance(index + 1);
            return new(unread.SliceUnsafe(0, index));
        }

        var builder = new InterpolatedU8StringHandler(unread.Length);
        Advance(unread.Length);
        builder.AppendBytes(unread);

        while ((unread = Fill()).Length > 0)
        {
            if ((index = unread.IndexOfAny(delimiters)) >= 0)
            {
                Advance(index + 1);
                builder.AppendBytes(unread.SliceUnsafe(0, index));
                break;
            }

            Advance(unread.Length);
            builder.AppendBytes(unread);
        }

        U8String.Validate(builder.Written);
        return new(ref builder);
    }

    public ValueTask<U8String?> ReadToAsync(byte delimiter, CancellationToken ct = default)
    {
        return ReadToAsyncCore(delimiter, ct);
    }

    public ValueTask<U8String?> ReadToAsync(char delimiter, CancellationToken ct = default)
    {
        ThrowHelpers.CheckSurrogate(delimiter);

        return ReadToAsyncCore(delimiter, ct);
    }

    public ValueTask<U8String?> ReadToAsync(Rune delimiter, CancellationToken ct = default)
    {
        return ReadToAsyncCore(delimiter, ct);
    }

    public ValueTask<U8String?> ReadToAsync(U8String delimiter, CancellationToken ct = default)
    {
        return ReadToAsyncCore(delimiter, ct);
    }

    // TODO: Okay, so it *is* the async depth and yield cost that ruins perfomance.
    async ValueTask<U8String?> ReadToAsyncCore<T>(T delimiter, CancellationToken ct)
        where T : struct
    {
        Debug.Assert(delimiter is not U8String s || !s.IsEmpty);

        var buffer = _buffer;
        var read = _bytesRead;
        var consumed = _bytesConsumed;
        if (consumed < read)
        {
            // Nothing to fill, the reader must consume
            // the unread bytes first.
            goto SkipRead;
        }

        // Check if we need to reset the buffer
        if (read >= buffer.Length)
        {
            Debug.Assert(consumed == read);
            read = 0;
            consumed = 0;
        }

        _bytesRead = read += await TSource.ReadAsync(
            this, buffer.AsMemory(read, buffer.Length - read), ct);
        _bytesConsumed = consumed;

    SkipRead:
        var unread = buffer.AsMemory(consumed, read - consumed);
        if (unread.Length <= 0)
        {
            return null;
        }

        var (index, length) = U8Searching.IndexOf(unread.Span, delimiter);
        if (index >= 0)
        {
            Advance(index + length);
            return new(unread.Span.SliceUnsafe(0, index));
        }

        var builder = new InterpolatedU8StringHandler(unread.Length);
        Advance(unread.Length);
        builder.AppendBytes(unread.Span);

        while (true)
        {
            read = _bytesRead;
            consumed = _bytesConsumed;

            if (consumed < read)
            {
                // Nothing to fill, the reader must consume
                // the unread bytes first.
                goto SkipRead2;
            }

            // Check if we need to reset the buffer
            if (read >= buffer.Length)
            {
                Debug.Assert(consumed == read);
                read = 0;
                consumed = 0;
            }

            _bytesRead = read += await TSource.ReadAsync(
                this, buffer.AsMemory(read, buffer.Length - read), ct);
            _bytesConsumed = consumed;

        SkipRead2:
            unread = buffer.AsMemory(consumed, read - consumed);

            if (unread.Length <= 0)
            {
                break;
            }

            (index, length) = U8Searching.IndexOf(unread.Span, delimiter);

            if (index >= 0)
            {
                Advance(index + length);
                builder.AppendBytes(unread.Span.SliceUnsafe(0, index));
                break;
            }

            Advance(unread.Length);
            builder.AppendBytes(unread.Span);
        }

        U8String.Validate(builder.Written);
        return new(ref builder);
    }

    public U8String? ReadToEnd()
    {
        throw new NotImplementedException();
    }

    public U8ReadResult TryRead(int length, out U8String value)
    {
        throw new NotImplementedException();
    }

    void Advance(int length)
    {
        _bytesConsumed += length;
    }

    // TODO: Support "stealing" the buffer with an adaptive strategy.
    // Likely when the read has fully populated it and it contains
    // delimiters or desired length/offset past 3/4 of its capacity.
    // Additionally, when EOF is reached it can be stolen if the difference
    // between sliced length and its size is below some reasonable threshold,
    // Maybe it's okay to just keep around the 4KB buffer for some odd 200B string?
    Span<byte> Fill()
    {
        var buffer = _buffer;
        var read = _bytesRead;
        var consumed = _bytesConsumed;

        if (consumed < read)
        {
            // Nothing to fill, the reader must consume
            // the unread bytes first.
            goto Done;
        }

        // Check if we need to reset the buffer
        if (read >= buffer.Length)
        {
            Debug.Assert(consumed == read);
            read = 0;
            consumed = 0;
        }

        _bytesRead = read += Source.Read(buffer.AsSpan(read, buffer.Length - read));
        _bytesConsumed = consumed;

    Done:
        return buffer.AsSpan(consumed, read - consumed);
    }

    async ValueTask<Memory<byte>> FillAsync(CancellationToken ct)
    {
        var buffer = _buffer;
        var read = _bytesRead;
        var consumed = _bytesConsumed;

        if (consumed < read)
        {
            // Nothing to fill, the reader must consume
            // the unread bytes first.
            goto Done;
        }

        // Check if we need to reset the buffer
        if (read >= buffer.Length)
        {
            Debug.Assert(consumed == read);
            read = 0;
            consumed = 0;
        }

        _bytesRead = read += await TSource.ReadAsync(
            this, buffer.AsMemory(read, buffer.Length - read), ct);
        _bytesConsumed = consumed;

    Done:
        return buffer.AsMemory(consumed, read - consumed);
    }

    public void Dispose()
    {
        Source.Dispose();
        GC.SuppressFinalize(this);
    }
}

public readonly struct U8LineReader<T>(U8Reader<T> reader) :
    IU8Enumerable<U8LineReader<T>.Enumerator>
        where T : struct, IU8ReaderSource<T>
{
    public Enumerator GetEnumerator() => new(reader);

    public struct Enumerator(U8Reader<T> reader) : IU8Enumerator
    {
        public U8String Current { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var line = reader.ReadTo((byte)'\n');
            if (line.HasValue)
            {
                Current = line.Value.StripSuffix((byte)'\r');
                return true;
            }

            return false;
        }

        public readonly void Dispose() => reader.Dispose();

        readonly object IEnumerator.Current => Current;
        readonly void IEnumerator.Reset() => throw new NotSupportedException();
    }

    IEnumerator<U8String> IEnumerable<U8String>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

// TODO: Look into CT interactions (and file reader too)
// TODO: Performs as fast as regular ReadLinesAsync which means
// there are implementation issues which leave a lot of perf on the table.
public readonly struct U8AsyncLineReader<T>(
    U8Reader<T> reader, CancellationToken ct = default) : IAsyncEnumerable<U8String>
        where T : struct, IU8ReaderSource<T>
{
    public AsyncEnumerator GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        cancellationToken = cancellationToken != default
            ? cancellationToken : ct;
        return new(reader, cancellationToken);
    }

    public sealed class AsyncEnumerator(
        U8Reader<T> reader, CancellationToken ct = default) :
            IAsyncEnumerator<U8String>
    {
        public U8String Current { get; private set; }

        public async ValueTask<bool> MoveNextAsync()
        {
            var line = await reader.ReadToAsync((byte)'\n', ct);
            if (line.HasValue)
            {
                Current = line.Value.StripSuffix((byte)'\r');
                return true;
            }

            return false;
        }

        public ValueTask DisposeAsync()
        {
            reader.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    IAsyncEnumerator<U8String> IAsyncEnumerable<U8String>.GetAsyncEnumerator(
        CancellationToken cancellationToken) => GetAsyncEnumerator(cancellationToken);
}
