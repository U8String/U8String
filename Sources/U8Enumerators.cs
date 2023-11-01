using System.Collections;
using System.Runtime.InteropServices;
using System.Text;

using U8Primitives.Abstractions;

namespace U8Primitives;

/// <summary>
/// A collection of chars in a provided <see cref="U8String"/>.
/// </summary>
public readonly struct U8Chars(U8String value) :
    ICollection<char>,
    IEnumerable<char, U8Chars.Enumerator>
{
    readonly U8String _value = value;

    /// <summary>
    /// The number of chars in the current <see cref="U8String"/>.
    /// </summary>
    public int Count
    {
        get
        {
            var value = _value;
            if (!value.IsEmpty)
            {
                return Encoding.UTF8.GetCharCount(value.UnsafeSpan);
            }

            return 0;
        }
    }

    public bool Contains(char item) => _value.Contains(item);

    public void CopyTo(Span<char> destination)
    {
        var value = _value;
        if (!value.IsEmpty)
        {
            Encoding.UTF8.GetChars(value.UnsafeSpan, destination);
        }
    }

    public void CopyTo(char[] destination, int index)
    {
        var value = _value;
        if (!value.IsEmpty)
        {
            Encoding.UTF8.GetChars(value.UnsafeSpan, destination.AsSpan()[index..]);
        }
    }

    public void Deconstruct(out char first, out char second)
    {
        this.Deconstruct<U8Chars, Enumerator, char>(out first, out second);
    }

    public void Deconstruct(out char first, out char second, out char third)
    {
        this.Deconstruct<U8Chars, Enumerator, char>(out first, out second, out third);
    }

    public char ElementAt(int index)
    {
        return this.ElementAt<U8Chars, Enumerator, char>(index);
    }

    public char ElementAtOrDefault(int index)
    {
        return this.ElementAtOrDefault<U8Chars, Enumerator, char>(index);
    }

    public char[] ToArray()
    {
        var (bytes, offset, length) = _value;

        if (bytes != null)
        {
            return Encoding.UTF8.GetChars(bytes, offset, length);
        }

        return [];
    }

    public List<char> ToList()
    {
        var value = _value;
        if (!value.IsEmpty)
        {
            var bytes = value.UnsafeSpan;
            var count = Encoding.UTF8.GetCharCount(bytes);
            var chars = new List<char>(count);
            CollectionsMarshal.SetCount(chars, count);
            var span = CollectionsMarshal.AsSpan(chars);

            Encoding.UTF8.GetChars(bytes, span);
            return chars;
        }

        return [];
    }

    public Enumerator GetEnumerator() => new(_value);

    IEnumerator<char> IEnumerable<char>.GetEnumerator() => new Enumerator(_value);
    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(_value);

    public struct Enumerator(U8String value) : IEnumerator<char>
    {
        readonly byte[]? _value = value._value;
        readonly U8Range _range = value._inner;
        int _nextByteIdx = 0;
        uint _currentCharPair;

        // TODO
        public readonly char Current => (char)_currentCharPair;

        // TODO: This is still terrible,
        // refactor to avoid UTF8->Rune->char conversion
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var (range, nextByteIdx, currentCharPair) =
                (_range, _nextByteIdx, _currentCharPair);

            if (currentCharPair < char.MaxValue)
            {
                if ((uint)nextByteIdx < (uint)range.Length)
                {
                    ref var ptr = ref _value!.AsRef(range.Offset + nextByteIdx);

                    if (U8Info.IsAsciiByte(in ptr))
                    {
                        _nextByteIdx = nextByteIdx + 1;
                        _currentCharPair = ptr;
                        return true;
                    }

                    var rune = U8Conversions.CodepointToRune(
                        ref ptr, out var size, checkAscii: false);
                    _nextByteIdx = nextByteIdx + size;

                    if (rune.IsBmp)
                    {
                        _currentCharPair = (uint)rune.Value;
                        return true;
                    }

                    // I wonder if this just explodes on BigEndian
                    var runeValue = (uint)rune.Value;
                    var highSurrogate = (char)((runeValue + ((0xD800u - 0x40u) << 10)) >> 10);
                    var lowSurrogate = (char)((runeValue & 0x3FFu) + 0xDC00u);
                    _currentCharPair = highSurrogate + ((uint)lowSurrogate << 16);
                    return true;
                }

                return false;
            }

            _currentCharPair = currentCharPair >> 16;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => _nextByteIdx = 0;

        readonly object IEnumerator.Current => Current;
        readonly void IDisposable.Dispose() { }
    }

    readonly bool ICollection<char>.IsReadOnly => true;
    readonly void ICollection<char>.Add(char item) => throw new NotSupportedException();
    readonly void ICollection<char>.Clear() => throw new NotSupportedException();
    readonly bool ICollection<char>.Remove(char item) => throw new NotSupportedException();
}

// TODO: Implement span-taking alternatives to work with ROS<T> where T in byte, char, Rune
/// <summary>
/// A collection of Runes (unicode scalar values) in a provided <see cref="U8String"/>.
/// </summary>
public readonly struct U8Runes(U8String value) :
    ICollection<Rune>,
    IEnumerable<Rune, U8Runes.Enumerator>
{
    readonly U8String _value = value;

    /// <summary>
    /// The number of Runes (unicode scalar values) in the current <see cref="U8String"/>.
    /// </summary>
    public int Count
    {
        get => _value.RuneCount;
    }

    public bool Contains(Rune item) => _value.Contains(item);

    public void CopyTo(Rune[] destination, int index)
    {
        // TODO: Rely on somewhat unreliable guarantee that Runes are UTF-32 scalar
        // values and implement bespoke SIMD UTF-8 -> UTF-32 transcoding
        this.CopyTo<U8Runes, Enumerator, Rune>(destination.AsSpan()[index..]);
    }

    public void Deconstruct(out Rune first, out Rune second)
    {
        this.Deconstruct<U8Runes, Enumerator, Rune>(out first, out second);
    }

    public void Deconstruct(out Rune first, out Rune second, out Rune third)
    {
        this.Deconstruct<U8Runes, Enumerator, Rune>(out first, out second, out third);
    }

    public Rune ElementAt(int index)
    {
        return this.ElementAt<U8Runes, Enumerator, Rune>(index);
    }

    public Rune ElementAtOrDefault(int index)
    {
        return this.ElementAtOrDefault<U8Runes, Enumerator, Rune>(index);
    }

    public Rune[] ToArray() => this.ToArray<U8Runes, Enumerator, Rune>();

    public List<Rune> ToList() => this.ToList<U8Runes, Enumerator, Rune>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(_value);

    IEnumerator<Rune> IEnumerable<Rune>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator(U8String value) : IEnumerator<Rune>
    {
        readonly byte[]? _value = value._value;
        readonly int _length = value._inner.Length;
        int _offset = value._inner.Offset;

        public Rune Current { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var offset = _offset;
            if (offset < _length)
            {
                ref var ptr = ref _value!.AsRef(offset);

                Current = U8Conversions.CodepointToRune(ref ptr, out var size);
                _offset = offset + size;
                return true;
            }

            return false;
        }

        readonly object IEnumerator.Current => Current;
        readonly void IEnumerator.Reset() => throw new NotSupportedException();
        readonly void IDisposable.Dispose() { }
    }

    bool ICollection<Rune>.IsReadOnly => true;
    void ICollection<Rune>.Add(Rune item) => throw new NotSupportedException();
    void ICollection<Rune>.Clear() => throw new NotSupportedException();
    bool ICollection<Rune>.Remove(Rune item) => throw new NotSupportedException();
}

// This is effectively https://github.com/dotnet/runtime/issues/28507 adapted to U8String
// Since the source has to be valid UTF-8, we can make assumptions which significantly improve performance.
/// <summary>
/// A collection of Rune indices in a provided <see cref="U8String"/>.
/// </summary>
public readonly struct U8RuneIndices(U8String value) :
    ICollection<U8RuneIndex>,
    IEnumerable<U8RuneIndex, U8RuneIndices.Enumerator>
{
    readonly U8String _value = value;

    public int Count
    {
        get => _value.RuneCount;
    }

    public bool Contains(U8RuneIndex item)
    {
        var value = _value;
        if (!value.IsEmpty)
        {
            var offset = item.Offset;

            if ((uint)offset < (uint)value.Length)
            {
                ref var ptr = ref value.UnsafeRefAdd(offset);

                return U8Info.RuneLength(in ptr) == item.Length &&
                    U8Conversions.CodepointToRune(ref ptr, out var _) == item.Value;
            }
        }

        return false;
    }

    public void CopyTo(U8RuneIndex[] destination, int index)
    {
        this.CopyTo<U8RuneIndices, Enumerator, U8RuneIndex>(destination.AsSpan()[index..]);
    }

    public void Deconstruct(out U8RuneIndex first, out U8RuneIndex second)
    {
        this.Deconstruct<U8RuneIndices, Enumerator, U8RuneIndex>(out first, out second);
    }

    public void Deconstruct(out U8RuneIndex first, out U8RuneIndex second, out U8RuneIndex third)
    {
        this.Deconstruct<U8RuneIndices, Enumerator, U8RuneIndex>(out first, out second, out third);
    }

    public U8RuneIndex ElementAt(int index)
    {
        return this.ElementAt<U8RuneIndices, Enumerator, U8RuneIndex>(index);
    }

    public U8RuneIndex ElementAtOrDefault(int index)
    {
        return this.ElementAtOrDefault<U8RuneIndices, Enumerator, U8RuneIndex>(index);
    }

    public U8RuneIndex[] ToArray() => this.ToArray<U8RuneIndices, Enumerator, U8RuneIndex>();

    public List<U8RuneIndex> ToList() => this.ToList<U8RuneIndices, Enumerator, U8RuneIndex>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(_value);

    IEnumerator<U8RuneIndex> IEnumerable<U8RuneIndex>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator(U8String value) : IEnumerator<U8RuneIndex>
    {
        readonly byte[]? _value = value._value;
        readonly int _length = value._inner.Length;
        int _offset = value._inner.Offset;

        public U8RuneIndex Current { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var offset = _offset;
            if (offset < _length)
            {
                ref var ptr = ref _value!.AsRef(offset);

                Current = new(
                    U8Conversions.CodepointToRune(ref ptr, out var size), offset, size);

                _offset = offset + size;
                return true;
            }

            return false;
        }

        readonly object IEnumerator.Current => Current;
        readonly void IEnumerator.Reset() => throw new NotSupportedException();
        readonly void IDisposable.Dispose() { }
    }

    bool ICollection<U8RuneIndex>.IsReadOnly => true;
    void ICollection<U8RuneIndex>.Add(U8RuneIndex item) => throw new NotSupportedException();
    void ICollection<U8RuneIndex>.Clear() => throw new NotSupportedException();
    bool ICollection<U8RuneIndex>.Remove(U8RuneIndex item) => throw new NotSupportedException();
}

/// <summary>
/// A collection of lines in a provided <see cref="U8String"/>.
/// </summary>
/// <param name="value">The string to enumerate over.</param>
public readonly struct U8Lines(U8String value) :
    ICollection<U8String>,
    IU8Enumerable<U8Lines.Enumerator>
{
    readonly U8String _value = value;

    /// <summary>
    /// The number of lines in the current <see cref="U8String"/>.
    /// </summary>
    public int Count
    {
        get
        {
            var value = _value;
            if (!value.IsEmpty)
            {
                return value.UnsafeSpan.Count((byte)'\n') + 1;
            }

            return 0;
        }
    }

    public bool Contains(U8String item)
    {
        return !item.Contains((byte)'\n') && _value.Contains(item);
    }

    public void CopyTo(U8String[] destination, int index)
    {
        this.CopyTo<U8Lines, Enumerator, U8String>(destination.AsSpan()[index..]);
    }

    public void Deconstruct(out U8String first, out U8String second)
    {
        this.Deconstruct<U8Lines, Enumerator, U8String>(out first, out second);
    }

    public void Deconstruct(out U8String first, out U8String second, out U8String third)
    {
        this.Deconstruct<U8Lines, Enumerator, U8String>(out first, out second, out third);
    }

    public U8String ElementAt(int index)
    {
        return this.ElementAt<U8Lines, Enumerator, U8String>(index);
    }

    public U8String ElementAtOrDefault(int index)
    {
        return this.ElementAtOrDefault<U8Lines, Enumerator, U8String>(index);
    }

    public U8String[] ToArray() => this.ToArray<U8Lines, Enumerator, U8String>();
    public List<U8String> ToList() => this.ToList<U8Lines, Enumerator, U8String>();

    /// <summary>
    /// Returns a <see cref="Enumerator"/> over the provided string.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(_value);

    IEnumerator<U8String> IEnumerable<U8String>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    bool ICollection<U8String>.IsReadOnly => true;
    void ICollection<U8String>.Add(U8String item) => throw new NotSupportedException();
    void ICollection<U8String>.Clear() => throw new NotSupportedException();
    bool ICollection<U8String>.Remove(U8String item) => throw new NotSupportedException();

    /// <summary>
    /// A struct that enumerates lines over a string.
    /// </summary>
    /// <param name="value">The string to enumerate over.</param>
    public struct Enumerator(U8String value) : IU8Enumerator
    {
        private readonly byte[]? _value = value._value;
        private U8Range _remaining = value._inner;
        private U8Range _current;

        /// <summary>
        /// Returns the current line.
        /// </summary>
        public readonly U8String Current => new(_value, _current.Offset, _current.Length);

        /// <summary>
        /// Advances the enumerator to the next line.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Surprisingly smaller codegen than when not inlined
        public bool MoveNext()
        {
            var remaining = _remaining;
            if (remaining.Length > 0)
            {
                var span = _value!.SliceUnsafe(remaining.Offset, remaining.Length);
                var idx = span.IndexOf((byte)'\n');

                if ((uint)idx < (uint)span.Length)
                {
                    var cutoff = idx;
                    if (idx > 0 && span.AsRef(idx - 1) is (byte)'\r')
                    {
                        cutoff--;
                    }

                    _current = new(remaining.Offset, cutoff);
                    _remaining = new(remaining.Offset + idx + 1, remaining.Length - idx - 1);
                }
                else
                {
                    // We've reached EOF, but we still need to return 'true' for this final
                    // iteration so that the caller can query the Current property once more.
                    _current = new(remaining.Offset, remaining.Length);
                    _remaining = default;
                }

                return true;
            }

            return false;
        }

        readonly object IEnumerator.Current => Current;
        readonly void IEnumerator.Reset() => throw new NotSupportedException();
        readonly void IDisposable.Dispose() { }
    }
}
