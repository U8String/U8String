using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using U8Primitives.InteropServices;

#pragma warning disable RCS1085, RCS1085FadeOut, IDE0032 // Use auto-implemented property. Why: readable fields.
namespace U8Primitives;

public readonly partial struct U8String
{
    public SplitPair SplitFirst(byte separator)
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

            return SplitPair.NotFound(source);
        }

        return default;
    }

    public SplitPair SplitFirst(char separator) => char.IsAscii(separator)
        ? SplitFirst((byte)separator)
        : SplitFirstUnchecked(separator.NonAsciiToUtf8(out _));

    public SplitPair SplitFirst(Rune separator) => separator.IsAscii
        ? SplitFirst((byte)separator.Value)
        : SplitFirstUnchecked(separator.NonAsciiToUtf8(out _));

    public SplitPair SplitFirst(U8String separator)
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

            return SplitPair.NotFound(source);
        }

        return default;
    }

    // It would be *really nice* to aggressively inline this method
    // but the way validation is currently implemented does not significantly
    // benefit from splitting on UTF-8 literals while possibly risking
    // running out of inlining budget significantly regressing performance everywhere else.
    public SplitPair SplitFirst(ReadOnlySpan<byte> separator)
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

            return SplitPair.NotFound(source);
        }

        return default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SplitPair SplitFirstUnchecked(ReadOnlySpan<byte> separator)
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

            return SplitPair.NotFound(source);
        }

        return default;
    }

    public SplitPair SplitLast(byte separator)
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

            return SplitPair.NotFound(source);
        }

        return default;
    }

    public SplitPair SplitLast(char separator) => char.IsAscii(separator)
        ? SplitLast((byte)separator)
        : SplitLastUnchecked(separator.NonAsciiToUtf8(out _));

    public SplitPair SplitLast(Rune separator) => separator.IsAscii
        ? SplitLast((byte)separator.Value)
        : SplitLastUnchecked(separator.NonAsciiToUtf8(out _));

    public SplitPair SplitLast(U8String separator)
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

            return SplitPair.NotFound(source);
        }

        return default;
    }

    public SplitPair SplitLast(ReadOnlySpan<byte> separator)
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

            return SplitPair.NotFound(source);
        }

        return default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SplitPair SplitLastUnchecked(ReadOnlySpan<byte> separator)
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

            return SplitPair.NotFound(source);
        }

        return default;
    }

    public U8Split Split(byte separator)
    {
        var split = default(U8Split);
        var source = this;
        if (!source.IsEmpty)
        {
            split = new(source, separator, 1);
        }

        return split;
    }

    public U8Split Split(char separator)
    {
        uint value;
        byte length;
        if (char.IsAscii(separator))
        {
            value = separator;
            length = 1;
        }
        else
        {
            length = (byte)(uint)separator.NonAsciiToUtf8(out value).Length;
        }

        var split = default(U8Split);
        var source = this;
        if (!source.IsEmpty)
        {
            split = new(source, value, length);
        }

        return split;
    }

    public U8Split Split(Rune separator)
    {
        uint value;
        byte length;
        if (separator.IsAscii)
        {
            value = (uint)separator.Value;
            length = 1;
        }
        else
        {
            length = (byte)(uint)separator.NonAsciiToUtf8(out value).Length;
        }

        var split = default(U8Split);
        var source = this;
        if (!source.IsEmpty)
        {
            split = new(source, value, length);
        }

        return split;
    }

    public U8Split<U8String> Split(U8String separator, U8SplitOptions options = U8SplitOptions.None)
    {
        var split = default(U8Split<U8String>);
        var source = this;
        if (!source.IsEmpty)
        {
            split = new(source, separator, options);
        }

        return split;
    }

    public U8Split<byte[]> Split(ReadOnlySpan<byte> separator, U8SplitOptions options = U8SplitOptions.None)
    {
        if (!IsValid(separator))
        {
            // TODO: EH UX
            ThrowHelpers.InvalidSplit();
        }

        var split = default(U8Split<byte[]>);
        var source = this;
        if (!source.IsEmpty)
        {
            split = new(source, separator.ToArray(), options);
        }

        return split;
    }
}

public readonly record struct SplitPair
{
    readonly U8String _value;
    readonly int _index;
    readonly int _separator;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal SplitPair(U8String value, int index, int separator)
    {
        _value = value;
        _index = index;
        _separator = separator;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SplitPair NotFound(U8String value)
    {
        return new(value, value.Length, 0);
    }

    public U8String Segment
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => U8Marshal.Slice(_value, 0, _index);
    }

    public U8String Remainder
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => U8Marshal.Slice(_value, _index + _separator);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out U8String segment, out U8String remainder)
    {
        segment = Segment;
        remainder = Remainder;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator (U8String, U8String)(SplitPair value)
    {
        return (value.Segment, value.Remainder);
    }
}

internal interface IU8Split<TEnumerator>
    where TEnumerator : struct, IEnumerator<U8String>
{
    TEnumerator GetEnumerator();
}

[StructLayout(LayoutKind.Auto)]
public readonly struct U8Split : ICollection<U8String>, IU8Split<U8Split.Enumerator>
{
    readonly U8String _value;
    readonly uint _separatorValue;
    readonly byte _separatorLength;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8Split(U8String value, uint separatorValue, byte separatorLength)
    {
        _value = value;
        _separatorValue = separatorValue;
        _separatorLength = separatorLength;
    }

    [UnscopedRef] // TODO: Is this UB?
    public readonly ReadOnlySpan<byte> Separator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return U8Splitting.CreateSeparator(in _separatorValue, _separatorLength);
        }
    }

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var value = _value;
            if (value.IsEmpty)
            {
                return 0;
            }

            return U8Searching.Count(value.UnsafeSpan, Separator) + 1;
        }
    }

    public readonly bool Contains(U8String item)
    {
        var overlaps = U8Searching.Contains(item, Separator);

        return !overlaps && _value.Contains(item);
    }

    public readonly void CopyTo(U8String[] destination, int index)
    {
        // TODO: Optimize
        // This one is "Tier 0", "Tier 1" would be double-pass with count into split ranges,
        // and "Tier 3" will be a single-pass open-coded loop
        foreach (var line in this)
        {
            destination[index++] = line;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out U8String first, out U8String second)
    {
        this.Deconstruct<U8Split, Enumerator>(out first, out second);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out U8String first, out U8String second, out U8String third)
    {
        this.Deconstruct<U8Split, Enumerator>(out first, out second, out third);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Enumerator GetEnumerator() => new(_value, _separatorValue, _separatorLength);

    readonly IEnumerator<U8String> IEnumerable<U8String>.GetEnumerator() => GetEnumerator();
    readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    readonly bool ICollection<U8String>.IsReadOnly => true;

    [StructLayout(LayoutKind.Auto)]
    public struct Enumerator : IEnumerator<U8String>
    {
        readonly byte[]? _value;
        readonly uint _separatorValue;
        readonly byte _separatorLength;
        U8Range _current;
        U8Range _remaining;

        internal Enumerator(U8String value, uint separatorValue, byte separatorLength)
        {
            if (!value.IsEmpty)
            {
                _value = value._value;
                _separatorValue = separatorValue;
                _separatorLength = separatorLength;
                _remaining = value._inner;
            }
        }

        public readonly U8String Current => new(_value, _current.Offset, _current.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var remaining = _remaining;
            if (remaining.Length > 0)
            {
                var value = _value!.SliceUnsafe(remaining.Offset, remaining.Length);
                var separator = U8Splitting.CreateSeparator(in _separatorValue, _separatorLength);

                var index = U8Searching.IndexOf(value, separator);
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

// TODO: I don't think this is a successful design. JIT/ILC can't optimize away
// ASCII char literal conversion causing the size of MoveNext() to explode. This needs an overhaul,
// possibly disambiguating on struct vs separator types or maybe even 3 options:
// primitive, span (ref split aka SplitRef? SplitSpan?) and U8String / byte[]
[StructLayout(LayoutKind.Auto)]
public struct U8Split<TSeparator> : ICollection<U8String>, IU8Split<U8Split<TSeparator>.Enumerator>
{
    readonly U8String _value;
    readonly TSeparator? _separator; // Maybe just box the separator to allow a union-like behavior?
    readonly U8SplitOptions _options;
    int _count;

    // TODO: Move value.IsEmpty -> count = 0 check here
    internal U8Split(
        U8String value,
        TSeparator? separator,
        U8SplitOptions options,
        int count = -1)
    {
        _value = value;
        _separator = separator;
        _options = options;
        _count = count;
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
            return _count = Count(_value, _separator, _options) + 1;

            static int Count(
                ReadOnlySpan<byte> value,
                TSeparator? separator,
                U8SplitOptions options)
            {
                if (options is U8SplitOptions.None)
                {
                    return U8Searching.Count(value, separator);
                }

                return U8Searching.Count(value, separator, options);
            }
        }
    }

    public readonly bool Contains(U8String item)
    {
        var separator = _separator;
        var overlaps = U8Searching.Contains(item, separator);

        return !overlaps && _value.Contains(item);
    }

    public readonly void CopyTo(U8String[] destination, int index)
    {
        // TODO: Optimize
        // This one is "Tier 0", "Tier 1" would be double-pass with count into split ranges,
        // and "Tier 3" will be a single-pass open-coded loop
        foreach (var line in this)
        {
            destination[index++] = line;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Deconstruct(out U8String first, out U8String second)
    {
        this.Deconstruct<U8Split<TSeparator>, Enumerator>(out first, out second);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Deconstruct(out U8String first, out U8String second, out U8String third)
    {
        this.Deconstruct<U8Split<TSeparator>, Enumerator>(out first, out second, out third);
    }

    /// <summary>
    /// Returns a <see cref="Enumerator"/> over the provided string.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Enumerator GetEnumerator() => new(_value, _separator, _options);

    readonly IEnumerator<U8String> IEnumerable<U8String>.GetEnumerator() => GetEnumerator();
    readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    readonly bool ICollection<U8String>.IsReadOnly => true;

    [StructLayout(LayoutKind.Auto)]
    public struct Enumerator : IEnumerator<U8String>
    {
        readonly byte[]? _value;
        readonly TSeparator? _separator;
        readonly U8SplitOptions _options;
        U8Range _current;
        U8Range _remaining;

        internal Enumerator(
            U8String value,
            TSeparator? separator,
            U8SplitOptions options)
        {
            if (!value.IsEmpty)
            {
                _value = value._value;
                _separator = separator;
                _options = options;
                _remaining = value._inner;
            }
        }

        public readonly U8String Current => new(_value, _current.Offset, _current.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var remaining = _remaining;
            if (remaining.Length > 0)
            {
                var value = _value!.SliceUnsafe(remaining.Offset, remaining.Length);
                var separator = U8Conversions.ToUtf8(_separator, out var _);
                var index = U8Searching.IndexOf(value, separator);
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

// // TODO:
// public ref struct U8RefSplit
// {
//     readonly U8String _value;
//     readonly ReadOnlySpan<byte> _separator;
//     // Can't force JIT/ILC to emit proper layout here which is sad, and explicit layout ruins codegen
//     // int _count;
// }