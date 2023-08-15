using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using U8Primitives.Abstractions;

using Rune = System.Text.Rune;

namespace U8Primitives;

#pragma warning disable IDE0032, IDE0057 // Use auto property and index operator. Why: Perf, struct layout, accuracy and codegen.
public readonly partial struct U8String
{
    /// <summary>
    /// Returns a collection of <see cref="char"/>s over the provided string.
    /// </summary>
    /// <remarks>
    /// This is a lazily-evaluated allocation-free collection.
    /// </remarks>
    public U8Chars Chars
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(this);
    }

    /// <summary>
    /// Returns a collection of <see cref="Rune"/>s over the provided string.
    /// </summary>
    /// <remarks>
    /// This is a lazily-evaluated allocation-free collection.
    /// </remarks>
    public U8Runes Runes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(this);
    }

    /// <summary>
    /// Returns a collection of lines over the provided string.
    /// </summary>
    /// <remarks>
    /// This is a lazily-evaluated allocation-free collection.
    /// </remarks>
    public U8Lines Lines
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(this);
    }

    // Bad codegen still :(
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);

    IEnumerator<byte> IEnumerable<byte>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IEnumerator<byte>
    {
        readonly byte[]? _value;
        readonly int _offset;
        readonly int _length;
        int _index;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator(U8String value)
        {
            _value = value._value;
            _offset = value.Offset;
            _length = value.Length;
            _index = -1;
        }

        // Still cheaper than MemoryMarshal clever variants
        public readonly byte Current => _value![_offset + _index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => (uint)(++_index) < (uint)_length;
        // {
        //     var index = _index;
        //     if (++index < _length)
        //     {
        //         // Current = Unsafe.Add(
        //         //     ref MemoryMarshal.GetArrayDataReference(_value!),
        //         //     (nint)(uint)(_offset + index));
        //         Current = _value![_offset + index];
        //         _index = index;
        //         return true;
        //     }

        //     return false;
        // }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset() => _index = -1;

        readonly object IEnumerator.Current => Current;
        readonly void IDisposable.Dispose() { }
    }
}

/// <summary>
/// A collection of chars in a provided <see cref="U8String"/>.
/// </summary>
public struct U8Chars : ICollection<char>
{
    readonly U8String _value;

    int _count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8Chars(U8String value)
    {
        if (!value.IsEmpty)
        {
            _value = value;
            _count = -1;
        }
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
            Encoding.UTF8.GetChars(value.UnsafeSpan, destination.AsSpan(index));
        }
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator(U8String value)
        {
            if (!value.IsEmpty)
            {
                _value = value._value;
                _offset = value.Offset;
                _length = value.Length;
                _nextByteIdx = 0;
            }
        }

        // TODO
        public readonly char Current => (char)_currentCharPair;

        // TODO: This looks terrible, there must be a better way
        // to convert UTF-8 to UTF-16 with an enumerator.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var (offset, length, nextByteIdx, currentCharPair) =
                (_offset, _length, _nextByteIdx, _currentCharPair);

            if (currentCharPair < char.MaxValue)
            {
                if ((uint)nextByteIdx < (uint)length)
                {
                    var span = _value!.SliceUnsafe(offset + nextByteIdx, length - nextByteIdx);
                    var firstByte = MemoryMarshal.GetReference(span);
                    if (U8Info.IsAsciiByte(firstByte))
                    {
                        // Fast path because Rune.DecodeFromUtf8 won't inline
                        // making UTF-8 push us more and more towards anglocentrism.
                        _nextByteIdx = nextByteIdx + 1;
                        _currentCharPair = firstByte;
                        return true;
                    }

                    var status = Rune.DecodeFromUtf8(span, out var rune, out var bytesConsumed);
                    Debug.Assert(status is OperationStatus.Done);

                    _nextByteIdx = nextByteIdx + bytesConsumed;

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

/// <summary>
/// A collection of Runes (unicode scalar values) in a provided <see cref="U8String"/>.
/// </summary>
public struct U8Runes : ICollection<Rune>
{
    readonly U8String _value;

    // If we bring up non-ascii counting to ascii level, we might not need this
    // similar to LineCollection.
    int _count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8Runes(U8String value)
    {
        if (!value.IsEmpty)
        {
            _value = value;
            _count = -1;
        }
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

            return _count = Count(_value.UnsafeSpan);

            static int Count(ReadOnlySpan<byte> value)
            {
                Debug.Assert(!value.IsEmpty);

                // TODO: SIMD non-continuation byte counting
                var runeCount = (int)(nint)Polyfills.Text.Ascii.GetIndexOfFirstNonAsciiByte(value);
                value = value.SliceUnsafe(runeCount);

                for (var i = 0; (uint)i < (uint)value.Length; i += U8Info.CharLength(value.AsRef(i)))
                {
                    runeCount++;
                }

                return runeCount;
            }
        }
    }

    public readonly bool Contains(Rune item) => _value.Contains(item);

    public readonly void CopyTo(Rune[] destination, int index)
    {
        // TODO: Consistency and correctness? Implement single-pass vectorized conversion?
        foreach (var rune in this)
        {
            destination[index++] = rune;
        }
    }

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Enumerator(U8String value)
        {
            if (!value.IsEmpty)
            {
                _value = value._value;
                _offset = value.Offset;
                _length = value.Length;
            }
        }

        public Rune Current { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            // TODO: Optimize for codegen, this one isn't great
            var index = _index;
            if (index < _length)
            {
                Rune.DecodeFromUtf8(
                    _value!.SliceUnsafe(_offset + index, _length - index),
                    out var rune,
                    out var consumed);

                Current = rune;
                _index = index + consumed;
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
    readonly void ICollection<Rune>.Add(Rune item) => throw new NotImplementedException();
    readonly void ICollection<Rune>.Clear() => throw new NotImplementedException();
    readonly bool ICollection<Rune>.Remove(Rune item) => throw new NotImplementedException();
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8Lines(U8String value)
    {
        if (!value.IsEmpty)
        {
            _value = value;
            _count = -1;
        }
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
        var count = Count;
        var dst = destination.AsSpan();
        if ((uint)count > (uint)dst.Length - (uint)index)
        {
            // TODO: EH UX
            ThrowHelpers.ArgumentOutOfRange();
        }

        if (count > 0)
        {
            foreach (var line in this)
            {
                dst[index++] = line;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Deconstruct(out U8String first, out U8String second)
    {
        this.Deconstruct<U8Lines, Enumerator>(out first, out second);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Deconstruct(out U8String first, out U8String second, out U8String third)
    {
        this.Deconstruct<U8Lines, Enumerator>(out first, out second, out third);
    }

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
            if (!value.IsEmpty)
            {
                _value = value._value;
                _remaining = value._inner;
            }
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
                    if (idx > 0 && span.AsRef().Add(idx - 1) is (byte)'\r')
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
