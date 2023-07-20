using System.Buffers;
using System.Collections;

namespace U8Primitives;

#pragma warning disable IDE0032 // Use auto property. Why: Perf, struct layout and accuracy.
public readonly partial struct U8String
{
    // Bad codegen, replace with custom implementation
    // Provide class variant for the interface implementation
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public ReadOnlySpan<byte>.Enumerator GetEnumerator() => AsSpan().GetEnumerator();

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
        private readonly U8String _value;

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
