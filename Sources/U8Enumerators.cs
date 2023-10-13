using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using U8Primitives.Abstractions;

namespace U8Primitives;

/// <summary>
/// A collection of chars in a provided <see cref="U8String"/>.
/// </summary>
public struct U8Chars : ICollection<char>, IEnumerable<char, U8Chars.Enumerator>
{
    readonly U8String _value;

    int _count;

    public U8Chars(U8String value)
    {
        _value = value;
        _count = value.IsEmpty ? 0 : -1;
    }

    /// <summary>
    /// The number of chars in the current <see cref="U8String"/>.
    /// </summary>
    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            // Somehow the codegen here is underwhelming
            var count = _count;
            if (count >= 0)
            {
                return count;
            }
            return _count = Count(_value.UnsafeSpan);

            static int Count(ReadOnlySpan<byte> value)
            {
                Debug.Assert(!value.IsEmpty);

                // TODO: Is this enough?
                return Encoding.UTF8.GetCharCount(value);
            }
        }
    }

    // TODO: Wow, this seems to be terribly broken on surrogate chars and 
    // there is no easy way to fix it without sacrificing performance.
    // Perhaps it is worth just do the transcoding iteration here and warn the users
    // instead of straight up producing UB or throwing exceptions???
    public readonly bool Contains(char item) => _value.Contains(item);

    public readonly void CopyTo(char[] destination, int index)
    {
        var value = _value;
        if (!value.IsEmpty)
        {
            Encoding.UTF8.GetChars(value.UnsafeSpan, destination.AsSpan()[index..]);
        }
    }

    public readonly void Deconstruct(out char first, out char second)
    {
        this.Deconstruct<U8Chars, Enumerator, char>(out first, out second);
    }

    public readonly void Deconstruct(out char first, out char second, out char third)
    {
        this.Deconstruct<U8Chars, Enumerator, char>(out first, out second, out third);
    }

    public readonly char ElementAt(int index)
    {
        return this.ElementAt<U8Chars, Enumerator, char>(index);
    }

    public readonly char ElementAtOrDefault(int index)
    {
        return this.ElementAtOrDefault<U8Chars, Enumerator, char>(index);
    }

    public char[] ToArray()
    {
        var value = _value;
        if (!value.IsEmpty)
        {
            var chars = new char[Count];
            Encoding.UTF8.GetChars(value.UnsafeSpan, chars);
            return chars;
        }

        return Array.Empty<char>();
    }

    public List<char> ToList()
    {
        var value = _value;
        if (!value.IsEmpty)
        {
            var count = Count;
            var chars = new List<char>(count);
            CollectionsMarshal.SetCount(chars, count);
            var span = CollectionsMarshal.AsSpan(chars);

            Encoding.UTF8.GetChars(value.UnsafeSpan, span);
            return chars;
        }

        return new List<char>();
    }

    public readonly Enumerator GetEnumerator() => new(_value);

    readonly IEnumerator<char> IEnumerable<char>.GetEnumerator() => new Enumerator(_value);
    readonly IEnumerator IEnumerable.GetEnumerator() => new Enumerator(_value);

    public struct Enumerator : IEnumerator<char>
    {
        // TODO: refactor layout
        readonly byte[]? _value;
        readonly int _offset;
        readonly int _length;
        int _nextByteIdx;
        uint _currentCharPair;

        public Enumerator(U8String value)
        {
            _value = value._value;
            _offset = value.Offset;
            _length = value.Length;
            _nextByteIdx = 0;
        }

        // TODO
        public readonly char Current => (char)_currentCharPair;

        // TODO: This is still terrible,
        // refactor to avoid UTF8->Rune->char conversion
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var (offset, length, nextByteIdx, currentCharPair) =
                (_offset, _length, _nextByteIdx, _currentCharPair);

            if (currentCharPair < char.MaxValue)
            {
                if ((uint)nextByteIdx < (uint)length)
                {
                    ref var ptr = ref _value!.AsRef(offset + nextByteIdx);

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
public struct U8Runes : ICollection<Rune>, IEnumerable<Rune, U8Runes.Enumerator>
{
    readonly U8String _value;

    // If we bring up non-ascii counting to ascii level, we might not need this
    // similar to LineCollection.
    int _count;

    public U8Runes(U8String value)
    {
        _value = value;
        _count = value.IsEmpty ? 0 : -1;
    }

    /// <summary>
    /// The number of Runes (unicode scalar values) in the current <see cref="U8String"/>.
    /// </summary>
    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            // Somehow the codegen here is underwhelming
            var count = _count;
            if (count >= 0)
            {
                return count;
            }

            return _count = _value.RuneCount;
        }
    }

    public readonly bool Contains(Rune item) => _value.Contains(item);

    public void CopyTo(Rune[] destination, int index)
    {
        // TODO: Rely on somewhat unreliable guarantee that Runes are UTF-32 scalar
        // values and implement bespoke SIMD UTF-8 -> UTF-32 transcoding
        this.CopyTo<U8Runes, Enumerator, Rune>(destination.AsSpan()[index..]);
    }

    public readonly void Deconstruct(out Rune first, out Rune second)
    {
        this.Deconstruct<U8Runes, Enumerator, Rune>(out first, out second);
    }

    public readonly void Deconstruct(out Rune first, out Rune second, out Rune third)
    {
        this.Deconstruct<U8Runes, Enumerator, Rune>(out first, out second, out third);
    }

    public readonly Rune ElementAt(int index)
    {
        return this.ElementAt<U8Runes, Enumerator, Rune>(index);
    }

    public readonly Rune ElementAtOrDefault(int index)
    {
        return this.ElementAtOrDefault<U8Runes, Enumerator, Rune>(index);
    }

    public Rune[] ToArray() => this.ToArray<U8Runes, Enumerator, Rune>();

    public List<Rune> ToList() => this.ToList<U8Runes, Enumerator, Rune>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Enumerator GetEnumerator() => new(_value);

    readonly IEnumerator<Rune> IEnumerable<Rune>.GetEnumerator() => GetEnumerator();
    readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<Rune>
    {
        readonly byte[]? _value;
        readonly int _offset;
        readonly int _length;
        int _index;

        public Enumerator(U8String value)
        {
            _value = value._value;
            _offset = value.Offset;
            _length = value.Length;
        }

        public Rune Current { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var index = _index;
            if (index < _length)
            {
                ref var ptr = ref _value!.AsRef(_offset + index);

                Current = U8Conversions.CodepointToRune(ref ptr, out var size);
                _index = index + size;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => _index = -1;

        readonly object IEnumerator.Current => Current;
        readonly void IDisposable.Dispose() { }
    }

    readonly bool ICollection<Rune>.IsReadOnly => true;
    readonly void ICollection<Rune>.Add(Rune item) => throw new NotSupportedException();
    readonly void ICollection<Rune>.Clear() => throw new NotSupportedException();
    readonly bool ICollection<Rune>.Remove(Rune item) => throw new NotSupportedException();
}

/// <summary>
/// A collection of lines in a provided <see cref="U8String"/>.
/// </summary>
public struct U8Lines : ICollection<U8String>, IU8Enumerable<U8Lines.Enumerator>
{
    readonly U8String _value;

    // We might not need this. Although counting is O(n), the absolute performance
    // is very good, and on AVX2/512 - it's basically instantenous.
    int _count;

    /// <summary>
    /// Creates a new line enumeration over the provided string.
    /// </summary>
    /// <param name="value">The string to enumerate over.</param>
    public U8Lines(U8String value)
    {
        _value = value;
        _count = value.IsEmpty ? 0 : -1;
    }

    /// <summary>
    /// The number of lines in the current <see cref="U8String"/>.
    /// </summary>
    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var count = _count;
            if (count >= 0)
            {
                return count;
            }

            // Matches the behavior of string.Split('\n').Length for "hello\n"
            // TODO: Should we break consistency and not count the very last segment if it is empty?
            // (likely no - an empty line is still a line)
            return _count = _value.UnsafeSpan.Count((byte)'\n') + 1;
        }
    }

    public readonly bool Contains(U8String item)
    {
        return !item.Contains((byte)'\n') && _value.Contains(item);
    }

    public void CopyTo(U8String[] destination, int index)
    {
        this.CopyTo<U8Lines, Enumerator, U8String>(destination.AsSpan()[index..]);
    }

    public readonly void Deconstruct(out U8String first, out U8String second)
    {
        this.Deconstruct<U8Lines, Enumerator, U8String>(out first, out second);
    }

    public readonly void Deconstruct(out U8String first, out U8String second, out U8String third)
    {
        this.Deconstruct<U8Lines, Enumerator, U8String>(out first, out second, out third);
    }

    public readonly U8String ElementAt(int index)
    {
        return this.ElementAt<U8Lines, Enumerator, U8String>(index);
    }

    public readonly U8String ElementAtOrDefault(int index)
    {
        return this.ElementAtOrDefault<U8Lines, Enumerator, U8String>(index);
    }

    public U8String[] ToArray() => this.ToArray<U8Lines, Enumerator, U8String>();
    public List<U8String> ToList() => this.ToList<U8Lines, Enumerator, U8String>();

    /// <summary>
    /// Returns a <see cref="Enumerator"/> over the provided string.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Enumerator GetEnumerator() => new(_value);

    readonly IEnumerator<U8String> IEnumerable<U8String>.GetEnumerator() => GetEnumerator();
    readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    readonly bool ICollection<U8String>.IsReadOnly => true;
    readonly void ICollection<U8String>.Add(U8String item) => throw new NotSupportedException();
    readonly void ICollection<U8String>.Clear() => throw new NotSupportedException();
    readonly bool ICollection<U8String>.Remove(U8String item) => throw new NotSupportedException();

    /// <summary>
    /// A struct that enumerates lines over a string.
    /// </summary>
    public struct Enumerator : IU8Enumerator
    {
        // TODO 1: Ensure this is aligned with Rust's .lines() implementation, or not?
        // private static readonly SearchValues<byte> NewLine = SearchValues.Create("\r\n"u8);
        // TODO 2: Consider using 'InnerOffsets'
        private readonly byte[]? _value;
        private U8Range _remaining;
        private U8Range _current;

        /// <summary>
        /// Creates a new line enumerator over the provided string.
        /// </summary>
        /// <param name="value">The string to enumerate over.</param>
        public Enumerator(U8String value)
        {
            _value = value._value;
            _remaining = value._inner;
        }

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
