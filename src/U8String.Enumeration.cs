using System.Buffers;
using System.Collections;
using System.Diagnostics;
using System.Text;

using Rune = System.Text.Rune;

namespace U8Primitives;

#pragma warning disable IDE0032, IDE0057 // Use auto property and index operator. Why: Perf, struct layout, accuracy and codegen.
public readonly partial struct U8String
{
    // Bad codegen still :(
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);

    IEnumerator<byte> IEnumerable<byte>.GetEnumerator() => new Enumerator(this);
    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

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
        public bool MoveNext() => ++_index < _length;
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

    /// <summary>
    /// Returns a collection of runes over the provided string.
    /// </summary>
    /// <remarks>
    /// The collection is lazily evaluated and is allocation-free.
    /// </remarks>
    /// <returns>A collection of runes.</returns>
    public RuneCollection Runes
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(this);
    }

    /// <summary>
    /// Returns a collection of lines over the provided string.
    /// </summary>
    /// <remarks>
    /// The collection is lazily evaluated and is allocation-free.
    /// </remarks>
    /// <returns>A collection of lines.</returns>
    public LineCollection Lines
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(this);
    }

    /// <summary>
    /// A collections of chars in a provided <see cref="U8String"/>.
    /// </summary>
    public struct CharCollection : ICollection<char>
    {
        readonly U8String _value;

        int _count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CharCollection(U8String value)
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

                    // TODO: Is this any different from RuneCollection.Count?
                    throw new NotImplementedException();
                }
            }
        }

        public readonly bool Contains(char item) => _value.Contains(item);

        public readonly void CopyTo(char[] destination, int index)
        {
            if (!_value.IsEmpty)
            {
                Encoding.UTF8.GetChars(_value.UnsafeSpan, destination.AsSpan(index));
            }
        }

        readonly bool ICollection<char>.IsReadOnly => throw new NotImplementedException();

        void ICollection<char>.Add(char item)
        {
            throw new NotImplementedException();
        }

        void ICollection<char>.Clear()
        {
            throw new NotImplementedException();
        }

        IEnumerator<char> IEnumerable<char>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        bool ICollection<char>.Remove(char item)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// A collection of Runes (unicode scalar values) in a provided <see cref="U8String"/>.
    /// </summary>
    public struct RuneCollection : ICollection<Rune>
    {
        readonly byte[]? _value;
        readonly int _offset;
        readonly int _length;

        // If we bring up non-ascii counting to ascii level, we might not need this
        // similar to LineCollection.
        int _count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RuneCollection(U8String value)
        {
            if (!value.IsEmpty)
            {
                _value = value._value;
                _offset = value.Offset;
                _length = value.Length;
                // -1 indicates non-empty non-default(T) RuneCollection
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
                return _count = Count(_value!.SliceUnsafe(_offset, _length));

                static int Count(ReadOnlySpan<byte> value)
                {
                    Debug.Assert(!value.IsEmpty);

                    var runeCount = (int)(nint)Polyfills.Text.Ascii.GetIndexOfFirstNonAsciiByte(value);

                    // TODO: Optimize after porting/copying Utf8Utility from dotnet/runtime
                    value = value.Slice(runeCount);
                    while (!value.IsEmpty)
                    {
                        var result = Rune.DecodeFromUtf8(value, out _, out var consumed);
                        Debug.Assert(result != OperationStatus.InvalidData);

                        runeCount++;
                        value = value.Slice(consumed);
                    }

                    return runeCount;
                }
            }
        }

        public readonly bool Contains(Rune item)
        {
            return new U8String(_value, _offset, _length)
                .Contains(item);
        }

        public readonly void CopyTo(Rune[] destination, int index)
        {
            // TODO: Consistency and correctness? Implement single-pass vectorized conversion?
            foreach (var rune in this)
            {
                destination[index++] = rune;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Enumerator GetEnumerator() => new(_value, _offset, _length);

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
                _value = value._value;
                _offset = value.Offset;
                _length = value.Length;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Enumerator(byte[]? value, int offset, int length)
            {
                _value = value;
                _offset = offset;
                _length = length;
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
    public struct LineCollection : ICollection<U8String>
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
        public LineCollection(U8String value)
        {
            _value = value;
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
                if (count != 0)
                {
                    return count;
                }

                return _count = _value
                    .AsSpan()
                    .Count((byte)'\n') + 1;
            }
        }

        public readonly bool Contains(U8String item)
        {
            return !item.Contains((byte)'\n') && _value.Contains(item);
        }

        public readonly void CopyTo(U8String[] destination, int index)
        {
            // TODO: Consistency and correctness?
            foreach (var line in this)
            {
                destination[index++] = line;
            }
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
        public struct Enumerator : IEnumerator<U8String>
        {
            // TODO: Ensure this is aligned with Rust's .lines() implementation, or not?
            // private static readonly SearchValues<byte> NewLine = SearchValues.Create("\r\n"u8);

            private readonly byte[]? _value;
            private int _remainingOffset;
            private int _remainingLength;
            private int _currentOffset;
            private int _currentLength;
            private bool _isEnumeratorActive;

            /// <summary>
            /// Creates a new line enumerator over the provided string.
            /// </summary>
            /// <param name="value">The string to enumerate over.</param>
            public Enumerator(U8String value)
            {
                if (!value.IsEmpty)
                {
                    (_value, _remainingOffset, _remainingLength) = (
                        value._value, value.Offset, value.Length);
                    (_currentOffset, _currentLength) = (0, 0);
                    _isEnumeratorActive = true;
                }
            }

            /// <summary>
            /// Returns the current line.
            /// </summary>
            public readonly U8String Current => new(_value, _currentOffset, _currentLength);
            readonly object IEnumerator.Current => new U8String(_value, _currentOffset, _currentLength);

            /// <summary>
            /// Advances the enumerator to the next line.
            /// </summary>
            public bool MoveNext()
            {
                if (_isEnumeratorActive)
                {
                    var (remainingOffset, remainingLength) = (_remainingOffset, _remainingLength);
                    var remaining = _value!.SliceUnsafe(remainingOffset, remainingLength);
                    var idx = remaining.IndexOf((byte)'\n');

                    if ((uint)idx < (uint)remaining.Length)
                    {
                        var stride = 1;
                        if (idx > 0 && remaining[idx - 1] is (byte)'\r')
                        {
                            stride = 2;
                        }

                        (_currentOffset, _currentLength) = (remainingOffset, idx);
                        (_remainingOffset, _remainingLength) = (
                            remainingOffset + idx + stride,
                            remainingLength - idx - stride);
                    }
                    else
                    {
                        // We've reached EOF, but we still need to return 'true' for this final
                        // iteration so that the caller can query the Current property once more.
                        (_currentOffset, _currentLength) = (remainingOffset, remainingLength);
                        (_remainingOffset, _remainingLength) = (0, 0);
                        _isEnumeratorActive = false;
                    }

                    return true;
                }

                return false;
            }

            readonly void IEnumerator.Reset() => throw new NotSupportedException();
            readonly void IDisposable.Dispose() { }
        }
    }
}
