using System.Collections;
using U8.Primitives;
using System.Text;

namespace U8.Prototypes;

// TODO: Flatten certain impl. bits to reduce inlining and locals pressure
// Design:
// - disallow empty separators
// - empty source yields single empty segment
[SkipLocalsInit]
readonly struct Split<T>: ICollection<U8String>
where T : struct {
    readonly U8String _source;
    readonly T _pattern;

    internal Split(U8String source, T pattern) {
        _source = source;
        _pattern = pattern;
    }

    public int Count {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _pattern.CountSegments(_source);
    }

    public bool Contains(U8String item) {
        // TODO: ContainsSegment
        throw new NotImplementedException();
    }

    // TODO: Optimize calling convention by moving to a static helper
    public int CopyTo(Span<U8String> destination) {
        var index = 0;
        foreach (var item in this) {
            destination[index++] = item;
        }
        return index + 1;
    }

    public int CopyTo(Span<U8Range> destination) {
        var index = 0;
        foreach (var item in this) {
            destination[index++] = item.Range;
        }
        return index + 1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(_source, _pattern);

    IEnumerator<U8String> IEnumerable<U8String>.GetEnumerator() => new Enumerator(_source, _pattern);
    IEnumerator IEnumerable.GetEnumerator() => new Enumerator(_source, _pattern);

    bool ICollection<U8String>.IsReadOnly => true;
    void ICollection<U8String>.Add(U8String item) => throw new NotSupportedException();
    void ICollection<U8String>.CopyTo(U8String[] array, int arrayIndex) => CopyTo(array.AsSpan(arrayIndex));
    void ICollection<U8String>.Clear() => throw new NotSupportedException();
    bool ICollection<U8String>.Remove(U8String item) => throw new NotSupportedException();

    [SkipLocalsInit]
    public struct Enumerator: IEnumerator<U8String> {
        readonly byte[] _bytes;
        readonly T _pattern;

        (int Offset, int Length) _current;
        (int Offset, int Length) _remainder;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(U8String source, T pattern) {
            _bytes = source._value ?? [];
            _pattern = pattern;
            _current = default;
            _remainder = (source._inner.Offset, source._inner.Length);
        }

        public readonly U8String Current {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_bytes, new(_current.Offset, _current.Length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() {
            var source = _remainder;
            if (source.Length > -1) {
                var span = _bytes.SliceUnsafe(source.Offset, source.Length);
                // Behold, the power of working around inlining issues by doing it manually!
                int offset, length, remainderOffset;
                if (_pattern is byte b) {
                    offset = 0;
                    length = span.IndexOf(b);
                    remainderOffset = length + 1;
                }
                else if (_pattern is char c and <= (char)0x7F) {
                    offset = 0;
                    length = span.IndexOf((byte)c);
                    remainderOffset = length + 1;
                }
                else if (_pattern is Rune { Value: <= 0x7F } r) {
                    offset = 0;
                    length = span.IndexOf((byte)r.Value);
                    remainderOffset = length + 1;
                }
                else (offset, length, remainderOffset) = _pattern.NextSegmentNonAscii(span);

                _current = (source.Offset + offset, length);
                _remainder = (
                    source.Offset + remainderOffset,
                    source.Length - remainderOffset);
                    
                if (length < 0) {
                    _current = source;
                    _remainder = (0, -1);
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

static class SplitExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Split<byte> NewSplit(this U8String source, byte pattern) {
        ThrowHelpers.CheckAscii(pattern);
        return new(source, pattern);
    }

    [SkipLocalsInit, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Split<char> NewSplit(this U8String source, char pattern) {
        ThrowHelpers.CheckSurrogate(pattern);
        return new(source, pattern);
    }

    public static Split<Rune> NewSplit(this U8String source, Rune pattern) {
        return new(source, pattern);
    }

    public static Split<U8String> NewSplit(this U8String source, U8String pattern) {
        return new(source, pattern);
    }

    // public static Split<EitherBytePattern> NewSplitAny(this U8String source, byte a, byte b) {
    //     ThrowHelpers.CheckAscii(a);
    //     ThrowHelpers.CheckAscii(b);
    //     return new(source, new(a, b));
    // }
}
