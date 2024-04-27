using System.Collections;
using System.Net.WebSockets;
using System.Text;

using U8.Abstractions;

namespace U8.IO;

// TODO: Write an analyzer to prevent resource leakage caused by
// creating and then discarding un-enumerated readers below.
// TODO: Consider making Line and Split readers direct enumerators instead?
public partial class U8Reader<TSource>
{
    // TODO: Both of these have consuming semantics, which is not ideal.
    // Ideally, the users should be able to write reader.ReadLines().Take(4).ToArray()
    // and then proceed using the reader which has advanced past those 4 lines regardless
    // whether it was previously used for other read operations or not.
    // Ideally #2, this should never be a footgun with PLINQ either.
    // TODO: Consider a design where cancellation is non-disposing/non-consuming
    public U8LineReader<TSource> Lines => new(this, disposeReader: true);

    /* public U8CharReader<TSource> Chars => new(this); */

    /* public U8RuneReader<TSource> Runes => new(this); */

    public U8SplitReader<TSource, byte> Split(byte separator)
    {
        return new(this, separator, disposeReader: true);
    }

    public U8SplitReader<TSource, char> Split(char separator)
    {
        ThrowHelpers.CheckSurrogate(separator);

        return new(this, separator, disposeReader: true);
    }

    public U8SplitReader<TSource, Rune> Split(Rune separator)
    {
        return new(this, separator, disposeReader: true);
    }

    public U8SplitReader<TSource, U8String> Split(U8String separator)
    {
        return new(this, separator, disposeReader: true);
    }
}

public readonly struct U8LineReader<T> :
    IU8Enumerable<U8LineReader<T>.Enumerator>,
    IAsyncEnumerable<U8String>
        where T : IU8ReaderSource
{
    readonly bool _disposeReader;

    // TODO: Or .Reader/.Source?
    public U8Reader<T> Value { get; }

    public U8LineReader(U8Reader<T> reader, bool disposeReader)
    {
        Value = reader;
        _disposeReader = disposeReader;
    }

    public Enumerator GetEnumerator() => new(Value, _disposeReader);

    public struct Enumerator(U8Reader<T> reader, bool disposeReader) : IU8Enumerator
    {
        public U8String Current { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            Current = default;
            var line = reader.ReadTo((byte)'\n');
            if (line.HasValue)
            {
                Current = line.Value.StripSuffix((byte)'\r');
                return true;
            }

            return false;
        }

        public readonly void Dispose()
        {
            if (disposeReader)
            {
                reader.Dispose();
            }
        }

        readonly object IEnumerator.Current => Current;
        readonly void IEnumerator.Reset() => throw new NotSupportedException();
    }

    public AsyncEnumerator GetAsyncEnumerator(CancellationToken ct = default)
    {
        return new(Value, _disposeReader, ct);
    }

    // TODO: Look into CT interactions (and file reader too)
    // TODO: Performs as fast as regular ReadLinesAsync which means
    // there are implementation issues which leave a lot of perf on the table.
    public sealed class AsyncEnumerator(
        U8Reader<T> reader, bool disposeReader, CancellationToken ct = default) :
            IAsyncEnumerator<U8String>
    {
        public U8String Current { get; private set; }

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        public async ValueTask<bool> MoveNextAsync()
        {
            Current = default;
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
            if (disposeReader)
            {
                reader.Dispose();
            }

            return ValueTask.CompletedTask;
        }
    }

    IEnumerator<U8String> IEnumerable<U8String>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    IAsyncEnumerator<U8String> IAsyncEnumerable<U8String>.GetAsyncEnumerator(
        CancellationToken cancellationToken) => GetAsyncEnumerator(cancellationToken);
}

public readonly struct U8SplitReader<T, TSeparator> :
    IU8Enumerable<U8SplitReader<T, TSeparator>.Enumerator>,
    IAsyncEnumerable<U8String>
        where T : IU8ReaderSource
        where TSeparator : struct
{
    readonly bool _disposeReader;

    public U8Reader<T> Value { get; }

    public TSeparator Separator { get; }

    internal U8SplitReader(U8Reader<T> reader, TSeparator separator, bool disposeReader)
    {
        Value = reader;
        Separator = separator;
        _disposeReader = disposeReader;
    }

    public Enumerator GetEnumerator()
    {
        return new(Value, Separator, _disposeReader);
    }

    public AsyncEnumerator GetAsyncEnumerator(CancellationToken ct = default)
    {
        return new(Value, Separator, _disposeReader, ct);
    }

    // TODO: Performance. Consider partially inlining the reader state considering
    // rather large default buffer size and assuming most iterations will not be going
    // through read calls so that the cost can be reduced to *almost* U8Split.Enumerator level.
    public struct Enumerator : IU8Enumerator
    {
        readonly U8Reader<T> _reader;
        readonly TSeparator _separator;
        readonly bool _disposeReader;

        public U8String Current { get; private set; }

        internal Enumerator(U8Reader<T> reader, TSeparator separator, bool disposeReader)
        {
            _reader = reader;
            _separator = separator;
            _disposeReader = disposeReader;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            Current = default;
            var segment = _reader.ReadTo(_separator);
            if (segment.HasValue)
            {
                Current = segment.Value;
                return true;
            }

            return false;
        }

        public readonly void Dispose()
        {
            if (_disposeReader)
            {
                _reader.Dispose();
            }
        }

        readonly object IEnumerator.Current => Current;
        readonly void IEnumerator.Reset() => throw new NotSupportedException();
    }

    public sealed class AsyncEnumerator : IAsyncEnumerator<U8String>
    {
        readonly U8Reader<T> _reader;
        readonly TSeparator _separator;
        readonly bool _disposeReader;
        readonly CancellationToken _ct;

        public U8String Current { get; private set; }

        internal AsyncEnumerator(
            U8Reader<T> reader,
            TSeparator separator,
            bool disposeReader,
            CancellationToken ct)
        {
            _reader = reader;
            _separator = separator;
            _disposeReader = disposeReader;
            _ct = ct;
        }

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        public async ValueTask<bool> MoveNextAsync()
        {
            Current = default;
            var segment = await _reader.ReadToAsync(_separator, _ct);
            if (segment.HasValue)
            {
                Current = segment.Value;
                return true;
            }

            return false;
        }

        public ValueTask DisposeAsync()
        {
            if (_disposeReader)
            {
                _reader.Dispose();
            }

            return ValueTask.CompletedTask;
        }
    }

    IEnumerator<U8String> IEnumerable<U8String>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    IAsyncEnumerator<U8String> IAsyncEnumerable<U8String>.GetAsyncEnumerator(
        CancellationToken cancellationToken) => GetAsyncEnumerator(cancellationToken);
}

public readonly struct U8SegmentReader<T, TSegment> : IAsyncEnumerable<U8String>
    where T : IU8SegmentedReaderSource<TSegment>
{
    readonly bool _disposeReader;

    public U8Reader<T> Value { get; }

    public U8SegmentReader(U8Reader<T> reader, bool disposeReader)
    {
        Value = reader;
        _disposeReader = disposeReader;
    }

    public AsyncEnumerator GetAsyncEnumerator(CancellationToken ct = default)
    {
        return new(Value, _disposeReader, ct);
    }

    public sealed class AsyncEnumerator(
        U8Reader<T> reader,
        bool disposeReader,
        CancellationToken ct) : IAsyncEnumerator<U8String>
    {
        readonly U8Reader<T> _reader = reader;
        readonly bool _disposeReader = disposeReader;
        readonly CancellationToken _ct = ct;

        public U8String Current { get; private set; }

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        public async ValueTask<bool> MoveNextAsync()
        {
            Current = default;
            var segment = await _reader.ReadSegmentAsync<T, TSegment>(_ct);
            if (segment.HasValue)
            {
                Current = segment.Value;
                return true;
            }

            return false;
        }

        public ValueTask DisposeAsync()
        {
            if (_disposeReader)
            {
                _reader.Dispose();
            }

            return ValueTask.CompletedTask;
        }
    }

    IAsyncEnumerator<U8String> IAsyncEnumerable<U8String>.GetAsyncEnumerator(
        CancellationToken cancellationToken) => GetAsyncEnumerator(cancellationToken);
}

public readonly struct U8WebSocketMessageReader : IAsyncEnumerable<U8String>
{
    readonly bool _disposeReader;

    public U8Reader<U8WebSocketSource> Value { get; }

    public U8WebSocketMessageReader(U8Reader<U8WebSocketSource> reader, bool disposeReader)
    {
        Value = reader;
        _disposeReader = disposeReader;
    }

    public AsyncEnumerator GetAsyncEnumerator(CancellationToken ct = default)
    {
        return new(Value, _disposeReader, ct);
    }

    public sealed class AsyncEnumerator(
        U8Reader<U8WebSocketSource> reader,
        bool disposeReader,
        CancellationToken ct) : IAsyncEnumerator<U8String>
    {
        readonly U8Reader<U8WebSocketSource> _reader = reader;
        readonly bool _disposeReader = disposeReader;
        readonly CancellationToken _ct = ct;

        public U8String Current { get; private set; }

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
        public async ValueTask<bool> MoveNextAsync()
        {
            Current = default;
            var segment = await _reader.ReadSegmentAsync<U8WebSocketSource, ValueWebSocketReceiveResult>(_ct);
            if (segment.HasValue)
            {
                Current = segment.Value;
                return true;
            }

            return false;
        }

        public ValueTask DisposeAsync()
        {
            if (_disposeReader)
            {
                _reader.Dispose();
            }

            return ValueTask.CompletedTask;
        }
    }

    IAsyncEnumerator<U8String> IAsyncEnumerable<U8String>.GetAsyncEnumerator(
        CancellationToken cancellationToken) => GetAsyncEnumerator(cancellationToken);
}
