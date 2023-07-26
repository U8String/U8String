using System.Buffers;
using System.Collections;
using System.Diagnostics;
using U8Primitives.Internals;

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
        readonly object IEnumerator.Current => Current;

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

        public readonly void Dispose() { }
    }

    public RuneCollection Runes => new(this);

    public struct RuneCollection : ICollection<Rune>
    {
        readonly byte[]? _value;
        readonly int _offset;
        readonly int _length;
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
        /// The number of <see cref="Rune"/>s in the current <see cref="U8String"/>.
        /// </summary>
        /// <returns>The number of <see cref="Rune"/>s.</returns>
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

                    var runeCount = (int)(nint)Ascii.GetIndexOfFirstNonAsciiByte(value);

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

        public readonly bool IsReadOnly => true;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Contains(Rune item)
        {
            return new U8String(_value, _offset, _length)
                .Contains(item);
        }

        public void CopyTo(Rune[] destination, int index)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            if ((uint)index > (uint)destination.Length)
            {
                ThrowHelpers.ArgumentOutOfRange(nameof(index));
            }

            ArgumentOutOfRangeException.ThrowIfLessThan(
                destination.Length - index, Count);

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
            readonly object IEnumerator.Current => Current;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext()
            {
                // TODO: Optimize for codegen, this one will be very slow
                var index = _index;
                if (index < _length && Rune.DecodeFromUtf8(
                    _value!.SliceUnsafe(_offset + index, _length - index),
                    out var rune,
                    out var consumed) is OperationStatus.Done)
                {
                    _index = index + consumed;
                    Current = rune;
                    return true;
                }

                return false;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset() => _index = -1;

            public readonly void Dispose() { }
        }

        readonly void ICollection<Rune>.Add(Rune item) => throw new NotImplementedException();
        readonly void ICollection<Rune>.Clear() => throw new NotImplementedException();
        readonly bool ICollection<Rune>.Remove(Rune item) => throw new NotImplementedException();
    }

    /// <summary>
    /// Returns an enumeration of lines over the provided string.
    /// </summary>
    /// <returns>An enumeration of lines.</returns>
    public LineEnumerable Lines
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(this);
    }

    /// <summary>
    /// An enumeration of lines over a string.
    /// </summary>
    public readonly struct LineEnumerable : IEnumerable<U8String>
    {
        readonly U8String _value;

        /// <summary>
        /// Creates a new line enumeration over the provided string.
        /// </summary>
        /// <param name="value">The string to enumerate over.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LineEnumerable(U8String value)
        {
            _value = value;
        }

        /// <summary>
        /// Returns a <see cref="Enumerator"/> over the provided string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Enumerator GetEnumerator() => new(_value);
        readonly IEnumerator<U8String> IEnumerable<U8String>.GetEnumerator() => GetEnumerator();
        readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// A struct that enumerates lines over a string.
        /// </summary>
        public struct Enumerator : IEnumerator<U8String>
        {
            // TODO: Ensure this is aligned with Rust's .lines() implementation, or not?
            // private static readonly SearchValues<byte> NewLine = SearchValues.Create("\r\n"u8);

            private readonly byte[]? _value;
            private InnerOffsets _remaining;
            private InnerOffsets _current;
            private bool _isEnumeratorActive;

            /// <summary>
            /// Creates a new line enumerator over the provided string.
            /// </summary>
            /// <param name="value">The string to enumerate over.</param>
            public Enumerator(U8String value)
            {
                if (!value.IsEmpty)
                {
                    _value = value._value;
                    _remaining = new(value.Offset, value.Length);
                    _current = default;
                    _isEnumeratorActive = true;
                }
            }

            /// <summary>
            /// Returns the current line.
            /// </summary>
            public readonly U8String Current => new(_value, _current.Offset, _current.Length);
            readonly object IEnumerator.Current => new U8String(_value, _current.Offset, _current.Length);

            /// <summary>
            /// Advances the enumerator to the next line.
            /// </summary>
            public bool MoveNext()
            {
                if (_isEnumeratorActive)
                {
                    var remOffsets = _remaining;
                    var remaining = _value!.SliceUnsafe(remOffsets.Offset, remOffsets.Length);
                    var idx = remaining.IndexOfAny((byte)'\n', (byte)'\r');

                    if ((uint)idx < (uint)remaining.Length)
                    {
                        var stride = 1;
                        if (remaining[idx] == (byte)'\r'
                            && idx + 1 < remaining.Length
                            && remaining[idx + 1] == (byte)'\n')
                        {
                            stride = 2;
                        }

                        _current = new(remOffsets.Offset, idx);
                        _remaining = new(
                            remOffsets.Offset + idx + stride,
                            remOffsets.Length - idx - stride);
                    }
                    else
                    {
                        // We've reached EOF, but we still need to return 'true' for this final
                        // iteration so that the caller can query the Current property once more.
                        _current = _remaining;
                        _remaining = default;
                        _isEnumeratorActive = false;
                    }

                    return true;
                }

                return false;
            }

            /// <summary>
            /// Not supported.
            /// </summary>
            /// <exception cref="NotSupportedException">Always thrown.</exception>
            public void Reset()
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// Not supported, is a no-op.
            /// </summary>
            public readonly void Dispose() { }
        }
    }
}
