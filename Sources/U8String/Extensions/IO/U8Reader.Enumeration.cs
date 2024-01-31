using System.Collections;
using System.Text;

using U8.Abstractions;

namespace U8.IO;

public partial class U8Reader<TSource>
{
    // TODO: Both of these have consuming semantics, which is not ideal.
    // Ideally, the users should be able to write reader.ReadLines().Take(4).ToArray()
    // and then proceed using the reader which has advanced past those 4 lines regardless
    // whether it was previously used for other read operations or not.
    // Ideally #2, this should never be a footgun with PLINQ either.
    // TODO: Consider a design where cancellation is non-disposing/non-consuming
    public U8LineReader<TSource> Lines => new(this);

    /* public U8CharReader<TSource> Chars => new(this); */

    /* public U8RuneReader<TSource> Runes => new(this); */

    public U8SplitReader<TSource, byte> Split(byte separator)
    {
        return new(this, separator);
    }

    public U8SplitReader<TSource, char> Split(char separator)
    {
        ThrowHelpers.CheckSurrogate(separator);

        return new(this, separator);
    }

    public U8SplitReader<TSource, Rune> Split(Rune separator)
    {
        return new(this, separator);
    }

    public U8SplitReader<TSource, U8String> Split(U8String separator)
    {
        return new(this, separator);
    }
}

public readonly struct U8LineReader<T>(U8Reader<T> reader) :
    IU8Enumerable<U8LineReader<T>.Enumerator>,
    IAsyncEnumerable<U8String>
        where T : struct, IDisposable
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

    public AsyncEnumerator GetAsyncEnumerator(CancellationToken ct = default)
    {
        return new(reader, ct);
    }

    // TODO: Look into CT interactions (and file reader too)
    // TODO: Performs as fast as regular ReadLinesAsync which means
    // there are implementation issues which leave a lot of perf on the table.
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

    IEnumerator<U8String> IEnumerable<U8String>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    IAsyncEnumerator<U8String> IAsyncEnumerable<U8String>.GetAsyncEnumerator(
        CancellationToken cancellationToken) => GetAsyncEnumerator(cancellationToken);
}

public readonly struct U8SplitReader<T, TSeparator> :
    IU8Enumerable<U8SplitReader<T, TSeparator>.Enumerator>,
    IAsyncEnumerable<U8String>
        where T : struct, IDisposable
        where TSeparator : struct
{
    readonly U8Reader<T> _reader;
    readonly TSeparator _separator;

    internal U8SplitReader(U8Reader<T> reader, TSeparator separator)
    {
        _reader = reader;
        _separator = separator;
    }

    public Enumerator GetEnumerator()
    {
        return new(_reader, _separator);
    }

    public AsyncEnumerator GetAsyncEnumerator(CancellationToken ct = default)
    {
        return new(_reader, _separator, ct);
    }

    public struct Enumerator : IU8Enumerator
    {
        readonly U8Reader<T> _reader;
        readonly TSeparator _separator;

        public U8String Current { get; private set; }

        internal Enumerator(U8Reader<T> reader, TSeparator separator)
        {
            _reader = reader;
            _separator = separator;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var segment = _reader.ReadTo(_separator);
            if (segment.HasValue)
            {
                Current = segment.Value;
                return true;
            }

            return false;
        }

        public readonly void Dispose() => _reader.Dispose();

        readonly object IEnumerator.Current => Current;
        readonly void IEnumerator.Reset() => throw new NotSupportedException();
    }

    public sealed class AsyncEnumerator : IAsyncEnumerator<U8String>
    {
        readonly U8Reader<T> _reader;
        readonly TSeparator _separator;
        readonly CancellationToken _ct;

        public U8String Current { get; private set; }

        internal AsyncEnumerator(
            U8Reader<T> reader,
            TSeparator separator,
            CancellationToken ct)
        {
            _reader = reader;
            _separator = separator;
            _ct = ct;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async ValueTask<bool> MoveNextAsync()
        {
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
            _reader.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    IEnumerator<U8String> IEnumerable<U8String>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    IAsyncEnumerator<U8String> IAsyncEnumerable<U8String>.GetAsyncEnumerator(
        CancellationToken cancellationToken) => GetAsyncEnumerator(cancellationToken);
}
