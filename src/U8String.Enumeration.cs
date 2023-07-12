using System.Buffers;
using System.Collections;
using U8Primitives.Unsafe;

namespace U8Primitives;

#pragma warning disable IDE0032 // Use auto property. Why: Perf, struct layout and accuracy.
public readonly partial struct U8String
{
    // Bad codegen, replace with custom implementation
    // Provide class variant for the interface implementation
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public ReadOnlySpan<byte>.Enumerator GetEnumerator() => AsSpan().GetEnumerator();

    public LineEnumerator Lines => new(this);

    // TODO: Questionable implementation copied from BCL that does not need to keep two copies fo an underlying buffer reference.
    // Rewrite to an efficient, bounds-check and validation free implementation.
    public struct LineEnumerator : IEnumerable<U8String>, IEnumerator<U8String>
    {
        private static readonly SearchValues<byte> NewLineChars = SearchValues.Create(U8Constants.NewLineChars);

        private readonly byte[]? _value;
        private (uint Offset, uint Length) _remaining;
        private (uint Offset, uint Length) _current;
        private bool _isEnumeratorActive;

        public LineEnumerator(U8String value)
        {
            if (!value.IsEmpty)
            {
                _value = value._value;
                _remaining = (value._offset, value._length);
                _current = default;
                _isEnumeratorActive = true;
            }
        }

        public readonly LineEnumerator GetEnumerator() => this;
        readonly IEnumerator<U8String> IEnumerable<U8String>.GetEnumerator() => this;
        readonly IEnumerator IEnumerable.GetEnumerator() => this;

        public readonly U8String Current => new(_value, _current.Offset, _current.Length);
        readonly object IEnumerator.Current => new U8String(_value, _current.Offset, _current.Length);

        public bool MoveNext()
        {
            if (!_isEnumeratorActive)
                return false;

            var remaining = _value.AsSpan((int)_remaining.Offset, (int)_remaining.Length);
            var idx = remaining.IndexOfAny(NewLineChars);

            if ((uint)idx < remaining.Length)
            {
                var stride = 1;
                if (remaining[idx] == (byte)'\r'
                    && idx + 1 < remaining.Length
                    && remaining[idx + 1] == (byte)'\n')
                {
                    stride = 2;
                }

                _current = (_remaining.Offset, (uint)idx);
                _remaining = (
                    (uint)(_remaining.Offset + idx + stride),
                    (uint)(_remaining.Length - idx - stride));
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

        public void Reset()
        {
            throw new NotSupportedException();
        }

        public readonly void Dispose() { }
    }
}
