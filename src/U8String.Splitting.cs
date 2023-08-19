using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using U8Primitives.Abstractions;
using U8Primitives.InteropServices;

#pragma warning disable RCS1085, RCS1085FadeOut, IDE0032 // Use auto-implemented property. Why: readable fields.
namespace U8Primitives;

public readonly partial struct U8String
{
    public U8SplitPair SplitFirst(byte separator)
    {
        if (!U8Info.IsAsciiByte(separator))
        {
            // TODO: EH UX
            ThrowHelpers.ArgumentOutOfRange();
        }

        var source = this;
        if (!source.IsEmpty)
        {
            var span = source.UnsafeSpan;
            var index = span.IndexOf(separator);
            if (index >= 0)
            {
                return new(source, index, 1);
            }

            return U8SplitPair.NotFound(source);
        }

        return default;
    }

    public U8SplitPair SplitFirst(char separator) => char.IsAscii(separator)
        ? SplitFirst((byte)separator)
        : SplitFirstUnchecked(separator.NonAsciiToUtf8(out _));

    public U8SplitPair SplitFirst(Rune separator) => separator.IsAscii
        ? SplitFirst((byte)separator.Value)
        : SplitFirstUnchecked(separator.NonAsciiToUtf8(out _));

    public U8SplitPair SplitFirst(U8String separator)
    {
        var source = this;
        if (!source.IsEmpty)
        {
            if (!separator.IsEmpty)
            {
                var span = source.UnsafeSpan;
                var index = span.IndexOf(separator.UnsafeSpan);
                if (index >= 0)
                {
                    return new(source, index, separator.Length);
                }
            }

            return U8SplitPair.NotFound(source);
        }

        return default;
    }

    // It would be *really nice* to aggressively inline this method
    // but the way validation is currently implemented does not significantly
    // benefit from splitting on UTF-8 literals while possibly risking
    // running out of inlining budget significantly regressing performance everywhere else.
    public U8SplitPair SplitFirst(ReadOnlySpan<byte> separator)
    {
        var source = this;
        if (!source.IsEmpty)
        {
            if (!separator.IsEmpty)
            {
                var span = source.UnsafeSpan;
                var index = span.IndexOf(separator);
                if (index >= 0)
                {
                    // Same as with Slice(int, int), this might dereference past the end of the string.
                    // TODO: Do something about it if it's ever an issue.
                    if (U8Info.IsContinuationByte(source.UnsafeRefAdd(index)) ||
                        U8Info.IsContinuationByte(source.UnsafeRefAdd(index + separator.Length)))
                    {
                        ThrowHelpers.InvalidSplit();
                    }

                    return new(source, index, separator.Length);
                }
            }

            return U8SplitPair.NotFound(source);
        }

        return default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8SplitPair SplitFirstUnchecked(ReadOnlySpan<byte> separator)
    {
        var source = this;
        if (!source.IsEmpty)
        {
            if (!separator.IsEmpty)
            {
                var span = source.UnsafeSpan;
                var index = span.IndexOf(separator);
                if (index >= 0)
                {
                    return new(source, index, separator.Length);
                }
            }

            return U8SplitPair.NotFound(source);
        }

        return default;
    }

    public U8SplitPair SplitLast(byte separator)
    {
        if (!U8Info.IsAsciiByte(separator))
        {
            // TODO: EH UX
            ThrowHelpers.ArgumentOutOfRange();
        }

        var source = this;
        if (!source.IsEmpty)
        {
            var span = source.UnsafeSpan;
            var index = span.LastIndexOf(separator);
            if (index >= 0)
            {
                return new(source, index, 1);
            }

            return U8SplitPair.NotFound(source);
        }

        return default;
    }

    public U8SplitPair SplitLast(char separator) => char.IsAscii(separator)
        ? SplitLast((byte)separator)
        : SplitLastUnchecked(separator.NonAsciiToUtf8(out _));

    public U8SplitPair SplitLast(Rune separator) => separator.IsAscii
        ? SplitLast((byte)separator.Value)
        : SplitLastUnchecked(separator.NonAsciiToUtf8(out _));

    public U8SplitPair SplitLast(U8String separator)
    {
        var source = this;
        if (!source.IsEmpty)
        {
            if (!separator.IsEmpty)
            {
                var span = source.UnsafeSpan;
                var index = span.LastIndexOf(separator.UnsafeSpan);
                if (index >= 0)
                {
                    return new(source, index, separator.Length);
                }
            }

            return U8SplitPair.NotFound(source);
        }

        return default;
    }

    public U8SplitPair SplitLast(ReadOnlySpan<byte> separator)
    {
        var source = this;
        if (!source.IsEmpty)
        {
            if (!separator.IsEmpty)
            {
                var span = source.UnsafeSpan;
                var index = span.LastIndexOf(separator);
                if (index >= 0)
                {
                    if (U8Info.IsContinuationByte(source.UnsafeRefAdd(index)) ||
                        U8Info.IsContinuationByte(source.UnsafeRefAdd(index + separator.Length)))
                    {
                        ThrowHelpers.InvalidSplit();
                    }

                    return new(source, index, separator.Length);
                }
            }

            return U8SplitPair.NotFound(source);
        }

        return default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8SplitPair SplitLastUnchecked(ReadOnlySpan<byte> separator)
    {
        var source = this;
        if (!source.IsEmpty)
        {
            if (!separator.IsEmpty)
            {
                var span = source.UnsafeSpan;
                var index = span.LastIndexOf(separator);
                if (index >= 0)
                {
                    return new(source, index, separator.Length);
                }
            }

            return U8SplitPair.NotFound(source);
        }

        return default;
    }

    public U8Split<byte> Split(byte separator)
    {
        if (!U8Info.IsAsciiByte(separator))
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        return new(this, separator);
    }

    public ConfiguredU8Split<byte> Split(byte separator, U8SplitOptions options)
    {
        if (!U8Info.IsAsciiByte(separator))
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        return new(this, separator, options);
    }

    public U8Split<char> Split(char separator)
    {
        if (char.IsSurrogate(separator))
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        return new(this, separator);
    }

    public ConfiguredU8Split<char> Split(char separator, U8SplitOptions options)
    {
        if (char.IsSurrogate(separator))
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        return new(this, separator, options);
    }

    public U8Split<Rune> Split(Rune separator) => new(this, separator);

    public ConfiguredU8Split<Rune> Split(Rune separator, U8SplitOptions options) => new(this, separator, options);

    public U8Split Split(U8String separator)
    {
        return !separator.IsEmpty ? new(this, separator) : default;
    }

    public ConfiguredU8Split<U8String> Split(U8String separator, U8SplitOptions options)
    {
        return !separator.IsEmpty ? new(this, separator, options) : default;
    }

    public U8Split<byte[]> Split(byte[] separator)
    {
        if (!IsValid(separator))
        {
            // TODO: EH UX
            ThrowHelpers.InvalidSplit();
        }

        var source = this;
        return (!source.IsEmpty && separator != null) ? new(source, separator) : default;
    }

    public ConfiguredU8Split<byte[]> Split(byte[] separator, U8SplitOptions options)
    {
        if (!IsValid(separator))
        {
            // TODO: EH UX
            ThrowHelpers.InvalidSplit();
        }

        var source = this;
        return (!source.IsEmpty && separator != null) ? new(source, separator, options) : default;
    }
}

public readonly record struct U8SplitPair
{
    readonly U8String _value;
    readonly int _offset;
    readonly int _stride;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8SplitPair(U8String value, int offset, int stride)
    {
        _value = value;
        _offset = offset;
        _stride = stride;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8SplitPair NotFound(U8String value)
    {
        return new(value, value.Length, 0);
    }

    public U8String Segment
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => U8Marshal.Slice(_value, 0, _offset);
    }

    public U8String Remainder
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => U8Marshal.Slice(_value, _offset + _stride);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out U8String segment, out U8String remainder)
    {
        segment = Segment;
        remainder = Remainder;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator (U8String, U8String)(U8SplitPair value)
    {
        return (value.Segment, value.Remainder);
    }
}

public struct U8Split : ICollection<U8String>, IU8Enumerable<U8Split.Enumerator>
{
    readonly U8String _value;
    readonly U8String _separator;
    int _count;

    internal U8Split(U8String value, U8String separator)
    {
        _value = value;
        _separator = separator;
        _count = value.IsEmpty ? 0 : -1;
    }

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
            return _count = Count(_value.UnsafeSpan, _separator) + 1;

            static int Count(ReadOnlySpan<byte> value, ReadOnlySpan<byte> separator)
            {
                return U8Searching.Count(value, separator);
            }
        }
    }

    public readonly bool Contains(U8String item)
    {
        var separator = _separator;
        var overlaps = item.Contains(separator);

        return !overlaps && _value.Contains(item);
    }

    public void CopyTo(U8String[] array, int index)
    {
        this.CopyTo<U8Split, Enumerator, U8String>(array.AsSpan()[index..]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Deconstruct(out U8String first, out U8String second)
    {
        this.Deconstruct<U8Split, Enumerator, U8String>(out first, out second);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Deconstruct(out U8String first, out U8String second, out U8String third)
    {
        this.Deconstruct<U8Split, Enumerator, U8String>(out first, out second, out third);
    }

    public U8String[] ToArray() => this.ToArray<U8Split, Enumerator, U8String>();
    public List<U8String> ToList() => this.ToList<U8Split, Enumerator, U8String>();

    /// <summary>
    /// Returns a <see cref="Enumerator"/> over the provided string.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Enumerator GetEnumerator() => new(_value, _separator);

    readonly IEnumerator<U8String> IEnumerable<U8String>.GetEnumerator() => GetEnumerator();
    readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    readonly bool ICollection<U8String>.IsReadOnly => true;

    public struct Enumerator : IU8Enumerator
    {
        readonly byte[]? _value;
        readonly U8String _separator;
        U8Range _current;
        U8Range _remaining;

        internal Enumerator(U8String value, U8String separator)
        {
            _value = value._value;
            _separator = separator;
            _remaining = value._inner;
        }

        public readonly U8String Current => new(_value, _current.Offset, _current.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var remaining = _remaining;
            if (remaining.Length > 0)
            {
                var value = _value!.SliceUnsafe(remaining.Offset, remaining.Length);
                var separator = _separator;
                var index = value.IndexOf(separator.UnsafeSpan);
                if (index >= 0)
                {
                    _current = new(remaining.Offset, index);
                    _remaining = new(
                        remaining.Offset + index + separator.Length,
                        remaining.Length - index - separator.Length);
                }
                else
                {
                    _current = remaining;
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

    readonly void ICollection<U8String>.Add(U8String item) => throw new NotSupportedException();
    readonly void ICollection<U8String>.Clear() => throw new NotSupportedException();
    readonly bool ICollection<U8String>.Remove(U8String item) => throw new NotSupportedException();
}

// TODO: Optimize even more. This design is far from the northstar of perfect codegen
// but it still somehow manages to outperform Rust split iterators
public struct U8Split<TSeparator> :
    ICollection<U8String>,
    IU8Enumerable<U8Split<TSeparator>.Enumerator>
{
    readonly U8String _value;
    readonly TSeparator? _separator;
    int _count;

    // TODO: validate/guard against unsupported generic instantiations
    // e.g.: foreach (var e in default(U8Split<object[]>)) must throw or at least not produce UB
    // Possible solution: just make the T -> byte[] cast throwing instead of reinterpret,
    // paying the price for splitting on boxed byte sequences which are not U8String.
    // This can be further mitigated by introducing proper U8RefSplit for splitting on
    // UTF-8 literals and other sources of ROS<byte> without forcing reallocation due to lifetime constraints.
    internal U8Split(U8String value, TSeparator? separator)
    {
        _value = value;
        _separator = separator;
        _count = value.IsEmpty ? 0 : -1;
    }

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
            return _count = Count(_value, _separator) + 1;

            static int Count(ReadOnlySpan<byte> value, TSeparator? separator)
            {
                return U8Searching.Count(value, separator);
            }
        }
    }

    public readonly bool Contains(U8String item)
    {
        var separator = _separator;
        var overlaps = U8Searching.Contains(item, separator);

        return !overlaps && _value.Contains(item);
    }

    public void CopyTo(U8String[] array, int index)
    {
        this.CopyTo<U8Split<TSeparator>, Enumerator, U8String>(array.AsSpan()[index..]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Deconstruct(out U8String first, out U8String second)
    {
        this.Deconstruct<U8Split<TSeparator>, Enumerator, U8String>(out first, out second);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Deconstruct(out U8String first, out U8String second, out U8String third)
    {
        this.Deconstruct<U8Split<TSeparator>, Enumerator, U8String>(out first, out second, out third);
    }

    public U8String[] ToArray() => this.ToArray<U8Split<TSeparator>, Enumerator, U8String>();
    public List<U8String> ToList() => this.ToList<U8Split<TSeparator>, Enumerator, U8String>();

    /// <summary>
    /// Returns a <see cref="Enumerator"/> over the provided string.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Enumerator GetEnumerator() => new(_value, _separator);

    readonly IEnumerator<U8String> IEnumerable<U8String>.GetEnumerator() => GetEnumerator();
    readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    readonly bool ICollection<U8String>.IsReadOnly => true;

    public struct Enumerator : IU8Enumerator
    {
        readonly byte[]? _value;
        readonly TSeparator? _separator;
        readonly U8Size _separatorSize;
        U8Range _current;
        U8Range _remaining;

        internal Enumerator(U8String value, TSeparator? separator)
        {
            _value = value._value;
            _separator = separator;
            _separatorSize = U8Info.GetSize(separator);
            _remaining = value._inner;
        }

        public readonly U8String Current => new(_value, _current.Offset, _current.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var remaining = _remaining;
            if (remaining.Length > 0)
            {
                var size = _separatorSize;
                var value = _value!.SliceUnsafe(remaining.Offset, remaining.Length);
                var index = U8Searching.IndexOf(value, _separator, size);
                if (index >= 0)
                {
                    _current = new(remaining.Offset, index);
                    _remaining = new(
                        remaining.Offset + index + (int)size,
                        remaining.Length - index - (int)size);
                }
                else
                {
                    _current = remaining;
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

    readonly void ICollection<U8String>.Add(U8String item) => throw new NotSupportedException();
    readonly void ICollection<U8String>.Clear() => throw new NotSupportedException();
    readonly bool ICollection<U8String>.Remove(U8String item) => throw new NotSupportedException();
}

public readonly struct ConfiguredU8Split<TSeparator> :
    IU8Enumerable<ConfiguredU8Split<TSeparator>.Enumerator>
{
    readonly U8String _value;
    readonly TSeparator? _separator;
    readonly U8SplitOptions _options;

    internal ConfiguredU8Split(U8String value, TSeparator? separator, U8SplitOptions options)
    {
        _value = value;
        _separator = separator;
        _options = options;
    }

    public readonly Enumerator GetEnumerator() => new(_value, _separator, _options);

    readonly IEnumerator<U8String> IEnumerable<U8String>.GetEnumerator() => GetEnumerator();
    readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IU8Enumerator
    {
        readonly byte[]? _value;
        readonly TSeparator? _separator;
        readonly U8Size _separatorSize;
        readonly U8SplitOptions _options;
        U8Range _current;
        U8Range _remaining;

        internal Enumerator(U8String value, TSeparator? separator, U8SplitOptions options)
        {
            _value = value._value;
            _separator = separator;
            _separatorSize = U8Info.GetSize(separator);
            _options = options;
            _remaining = value._inner;
        }

        public readonly U8String Current => new(_value, _current.Offset, _current.Length);

        // TODO: Not most efficient but it works for now
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
        Next:
            var remaining = _remaining;
            if (remaining.Length > 0)
            {
                var size = _separatorSize;
                var value = _value!.SliceUnsafe(remaining.Offset, remaining.Length);
                var index = U8Searching.IndexOf(value, _separator, size);
                if (index >= 0)
                {
                    _current = (_options & U8SplitOptions.Trim) != U8SplitOptions.Trim
                        ? new(remaining.Offset, index)
                        : TrimEntry(_value!, new(remaining.Offset, index));
                    _remaining = new(
                        remaining.Offset + index + (int)size,
                        remaining.Length - index - (int)size);
                }
                else
                {
                    _current = (_options & U8SplitOptions.Trim) != U8SplitOptions.Trim
                        ? remaining
                        : TrimEntry(_value!, remaining);
                    _remaining = default;
                }

                if ((_options & U8SplitOptions.RemoveEmpty) is U8SplitOptions.RemoveEmpty
                    && _current.Length is 0)
                {
                    goto Next;
                }

                return true;
            }

            return false;
        }

        private static U8Range TrimEntry(byte[] value, U8Range range)
        {
            // This could have been done better but works for now.
            return new U8String(value, range).Trim()._inner;
        }

        readonly object IEnumerator.Current => Current;
        readonly void IEnumerator.Reset() => throw new NotSupportedException();
        readonly void IDisposable.Dispose() { }
    }
}

// // TODO:
// public ref struct U8RefSplit
// {
//     readonly U8String _value;
//     readonly ReadOnlySpan<byte> _separator;
//     // Can't force JIT/ILC to emit proper layout here which is sad, and explicit layout ruins codegen
//     // int _count;
// }