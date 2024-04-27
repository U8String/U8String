using System.Collections;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

using U8.Abstractions;
using U8.Shared;

namespace U8.Primitives;

#pragma warning disable IDE0032, RCS1085 // Use auto property. Why: explict struct contents annotation.
public readonly record struct U8SplitPair
{
    readonly byte[]? _value;
    readonly U8Range _segment;
    readonly U8Range _remainder;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8SplitPair(U8String value, int offset, int stride)
    {
        _value = value._value;
        _segment = new(value.Offset, offset);
        _remainder = new(
            value.Offset + offset + stride,
            value.Length - offset - stride);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8SplitPair(byte[]? value, U8Range segment, U8Range remainder)
    {
        _value = value;
        _segment = segment;
        _remainder = remainder;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static U8SplitPair NotFound(U8String value)
    {
        return new(value, value.Length, 0);
    }

    public U8String Segment
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_value, _segment);
    }

    public U8String Remainder
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_value, _remainder);
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

public readonly struct U8Split(U8String value, U8String separator) :
    IU8Enumerable<U8Split.Enumerator>,
    IU8SliceCollection
{
    readonly U8String _value = value;
    readonly U8String _separator = separator;

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var deref = this;
            if (!deref._value.IsEmpty)
            {
                if (!deref._separator.IsEmpty)
                {
                    return U8Searching.Count(
                        deref._value.UnsafeSpan, deref._separator.UnsafeSpan) + 1;
                }

                return 1;
            }

            return 0;
        }
    }

    public U8String Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _value;
    }

    public U8String Separator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _separator;
    }

    public bool Contains(U8String item)
    {
        return U8Searching.ContainsSegment(
            haystack: _value,
            needle: item,
            separator: _separator);
    }

    public bool Contains(ReadOnlySpan<byte> item)
    {
        return U8Searching.ContainsSegment(
            haystack: _value,
            needle: item,
            separator: _separator);
    }

    public void CopyTo(U8String[] array, int index)
    {
        this.CopyTo<U8Split, Enumerator, U8String>(array.AsSpan()[index..]);
    }

    public void Deconstruct(out U8String first, out U8String second)
    {
        this.Deconstruct<U8Split, Enumerator, U8String>(out first, out second);
    }

    public void Deconstruct(out U8String first, out U8String second, out U8String third)
    {
        this.Deconstruct<U8Split, Enumerator, U8String>(out first, out second, out third);
    }

    public U8String ElementAt(int index)
    {
        return this.ElementAt<U8Split, Enumerator, U8String>(index);
    }

    public U8String ElementAtOrDefault(int index)
    {
        return this.ElementAtOrDefault<U8Split, Enumerator, U8String>(index);
    }

    public int FindOffset(U8String item)
    {
        return U8Searching.IndexOfSegment(
            haystack: _value,
            needle: item,
            separator: _separator);
    }

    public int FindOffset(ReadOnlySpan<byte> item)
    {
        return U8Searching.IndexOfSegment(
            haystack: _value,
            needle: item,
            separator: _separator);
    }

    public U8String[] ToArray() => this.ToArray<U8Split, Enumerator, U8String>();
    public ImmutableArray<U8String> ToImmutableArray() => ImmutableCollectionsMarshal.AsImmutableArray(ToArray());
    public List<U8String> ToList() => this.ToList<U8Split, Enumerator, U8String>();
    public U8Slices ToSlices() => this.ToSlices<U8Split, Enumerator>();

    /// <summary>
    /// Returns a <see cref="Enumerator"/> over the provided string.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(_value, _separator);

    IEnumerator<U8String> IEnumerable<U8String>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    bool ICollection<U8String>.IsReadOnly => true;

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

        public readonly U8String Current => new(_value, _current);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var remaining = _remaining;
            if (remaining.Length > 0)
            {
                var value = _value!.SliceUnsafe(remaining.Offset, remaining.Length);
                var separator = _separator;
                var index = value.IndexOf(separator);
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

    void ICollection<U8String>.Add(U8String item) => throw new NotSupportedException();
    void ICollection<U8String>.Clear() => throw new NotSupportedException();
    bool ICollection<U8String>.Remove(U8String item) => throw new NotSupportedException();
}

// Beats Rust's split iterator by a wide margin :)
public readonly struct U8Split<TSeparator> :
    IU8Enumerable<U8Split<TSeparator>.Enumerator>,
    IU8SliceCollection
        where TSeparator : unmanaged
{
    readonly U8String _value;
    readonly TSeparator _separator;

    internal U8Split(U8String value, TSeparator separator)
    {
        _value = value;
        _separator = separator;
    }

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var value = _value;
            return !value.IsEmpty
                ? U8Searching.Count(value.UnsafeSpan, _separator) + 1 : 0;
        }
    }

    public U8String Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _value;
    }

    public TSeparator Separator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _separator;
    }

    public bool Contains(U8String item)
    {
        return U8Searching.ContainsSegment(
            haystack: _value,
            needle: item,
            separator: _separator);
    }

    public bool Contains(ReadOnlySpan<byte> item)
    {
        return U8Searching.ContainsSegment(
            haystack: _value,
            needle: item,
            separator: _separator);
    }

    public void CopyTo(U8String[] array, int index)
    {
        this.CopyTo<U8Split<TSeparator>, Enumerator, U8String>(array.AsSpan()[index..]);
    }

    public void Deconstruct(out U8String first, out U8String second)
    {
        this.Deconstruct<U8Split<TSeparator>, Enumerator, U8String>(out first, out second);
    }

    public void Deconstruct(out U8String first, out U8String second, out U8String third)
    {
        this.Deconstruct<U8Split<TSeparator>, Enumerator, U8String>(out first, out second, out third);
    }

    public U8String ElementAt(int index)
    {
        return this.ElementAt<U8Split<TSeparator>, Enumerator, U8String>(index);
    }

    public U8String ElementAtOrDefault(int index)
    {
        return this.ElementAtOrDefault<U8Split<TSeparator>, Enumerator, U8String>(index);
    }

    public int FindOffset(U8String item)
    {
        return U8Searching.IndexOfSegment(
            haystack: _value,
            needle: item,
            separator: _separator);
    }

    public int FindOffset(ReadOnlySpan<byte> item)
    {
        return U8Searching.IndexOfSegment(
            haystack: _value,
            needle: item,
            separator: _separator);
    }

    public U8String[] ToArray() => this.ToArray<U8Split<TSeparator>, Enumerator, U8String>();
    public ImmutableArray<U8String> ToImmutableArray() => ImmutableCollectionsMarshal.AsImmutableArray(ToArray());
    public List<U8String> ToList() => this.ToList<U8Split<TSeparator>, Enumerator, U8String>();
    public U8Slices ToSlices() => this.ToSlices<U8Split<TSeparator>, Enumerator>();

    /// <summary>
    /// Returns a <see cref="Enumerator"/> over the provided string.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(_value, _separator);

    IEnumerator<U8String> IEnumerable<U8String>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    bool ICollection<U8String>.IsReadOnly => true;

    public struct Enumerator : IU8Enumerator
    {
        readonly byte[]? _value;
        readonly TSeparator _separator;
        U8Range _current;
        U8Range _remaining;

        internal Enumerator(U8String value, TSeparator separator)
        {
            _value = value._value;
            _separator = separator;
            _remaining = value._inner;
        }

        public readonly U8String Current => new(_value, _current);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var remaining = _remaining;
            if (remaining.Length > 0)
            {
                var value = _value!.SliceUnsafe(remaining.Offset, remaining.Length);
                var (index, length) = U8Searching.IndexOf(value, _separator);
                if (index >= 0)
                {
                    _current = new(remaining.Offset, index);
                    _remaining = new(
                        remaining.Offset + index + length,
                        remaining.Length - index - length);
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

    void ICollection<U8String>.Add(U8String item) => throw new NotSupportedException();
    void ICollection<U8String>.Clear() => throw new NotSupportedException();
    bool ICollection<U8String>.Remove(U8String item) => throw new NotSupportedException();
}

public readonly struct U8Split<TSeparator, TComparer> :
    IU8Enumerable<U8Split<TSeparator, TComparer>.Enumerator>,
    IU8SliceCollection
        where TSeparator : struct
        where TComparer : IU8Comparer
{
    readonly U8String _value;
    readonly TSeparator _separator;
    readonly TComparer _comparer;

    internal U8Split(U8String value, TSeparator separator, TComparer comparer)
    {
        _value = value;
        _separator = separator;
        _comparer = comparer;
    }

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var value = _value;
            return !value.IsEmpty
                ? U8Searching.Count(value.UnsafeSpan, _separator, _comparer) + 1 : 0;
        }
    }

    public U8String Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _value;
    }

    public bool Contains(U8String item)
    {
        return U8Searching.ContainsSegment(
            haystack: _value,
            needle: item,
            separator: _separator,
            comparer: _comparer);
    }

    public bool Contains(ReadOnlySpan<byte> item)
    {
        return U8Searching.ContainsSegment(
            haystack: _value,
            needle: item,
            separator: _separator,
            comparer: _comparer);
    }

    public void CopyTo(U8String[] array, int index)
    {
        this.CopyTo<U8Split<TSeparator, TComparer>, Enumerator, U8String>(array.AsSpan()[index..]);
    }

    public void Deconstruct(out U8String first, out U8String second)
    {
        this.Deconstruct<U8Split<TSeparator, TComparer>, Enumerator, U8String>(out first, out second);
    }

    public void Deconstruct(out U8String first, out U8String second, out U8String third)
    {
        this.Deconstruct<U8Split<TSeparator, TComparer>, Enumerator, U8String>(out first, out second, out third);
    }

    public U8String ElementAt(int index)
    {
        return this.ElementAt<U8Split<TSeparator, TComparer>, Enumerator, U8String>(index);
    }

    public U8String ElementAtOrDefault(int index)
    {
        return this.ElementAtOrDefault<U8Split<TSeparator, TComparer>, Enumerator, U8String>(index);
    }

    public U8String[] ToArray() => this.ToArray<U8Split<TSeparator, TComparer>, Enumerator, U8String>();
    public ImmutableArray<U8String> ToImmutableArray() => ImmutableCollectionsMarshal.AsImmutableArray(ToArray());
    public List<U8String> ToList() => this.ToList<U8Split<TSeparator, TComparer>, Enumerator, U8String>();

    /// <summary>
    /// Returns a <see cref="Enumerator"/> over the provided string.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(_value, _separator, _comparer);

    IEnumerator<U8String> IEnumerable<U8String>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    bool ICollection<U8String>.IsReadOnly => true;

    public struct Enumerator : IU8Enumerator
    {
        readonly byte[]? _value;
        readonly TSeparator _separator;
        readonly TComparer _comparer;
        U8Range _current;
        U8Range _remaining;

        internal Enumerator(U8String value, TSeparator separator, TComparer comparer)
        {
            _value = value._value;
            _separator = separator;
            _comparer = comparer;
            _remaining = value._inner;
        }
        public readonly U8String Current => new(_value, _current);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var remaining = _remaining;
            if (remaining.Length > 0)
            {
                var value = _value!.SliceUnsafe(remaining.Offset, remaining.Length);
                var (index, length) = U8Searching.IndexOf(value, _separator, _comparer);
                if (index >= 0)
                {
                    _current = new(remaining.Offset, index);
                    _remaining = new(
                        remaining.Offset + index + length,
                        remaining.Length - index - length);
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

    void ICollection<U8String>.Add(U8String item) => throw new NotSupportedException();
    void ICollection<U8String>.Clear() => throw new NotSupportedException();
    bool ICollection<U8String>.Remove(U8String item) => throw new NotSupportedException();
}

public readonly struct ConfiguredU8Split<TSeparator, TOptions> :
    IU8Enumerable<ConfiguredU8Split<TSeparator, TOptions>.Enumerator>,
    IU8Split<TSeparator>
        where TSeparator : struct
        where TOptions : unmanaged, IU8SplitOptions
{
    readonly U8String _value;
    readonly TSeparator _separator;

    public U8String Value => _value;
    public TSeparator Separator => _separator;

    internal ConfiguredU8Split(U8String value, TSeparator separator)
    {
        _value = value;
        _separator = separator;
    }

    public void Deconstruct(out U8String first, out U8String second)
    {
        this.Deconstruct<ConfiguredU8Split<TSeparator, TOptions>, Enumerator, U8String>(out first, out second);
    }

    public void Deconstruct(out U8String first, out U8String second, out U8String third)
    {
        this.Deconstruct<ConfiguredU8Split<TSeparator, TOptions>, Enumerator, U8String>(out first, out second, out third);
    }

    public U8String ElementAt(int index)
    {
        return this.ElementAt<ConfiguredU8Split<TSeparator, TOptions>, Enumerator, U8String>(index);
    }

    public U8String ElementAtOrDefault(int index)
    {
        return this.ElementAtOrDefault<ConfiguredU8Split<TSeparator, TOptions>, Enumerator, U8String>(index);
    }

    public U8String[] ToArray()
    {
        var hint = U8Searching.Count(_value, _separator);
        return this.ToArrayUnsized<ConfiguredU8Split<TSeparator, TOptions>, Enumerator, U8String>(hint);
    }

    public ImmutableArray<U8String> ToImmutableArray() => ImmutableCollectionsMarshal.AsImmutableArray(ToArray());

    public List<U8String> ToList()
    {
        var hint = U8Searching.Count(_value, _separator);
        return this.ToListUnsized<ConfiguredU8Split<TSeparator, TOptions>, Enumerator, U8String>(hint);
    }

    public Enumerator GetEnumerator() => new(_value, _separator);

    IEnumerator<U8String> IEnumerable<U8String>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IU8Enumerator
    {
        readonly byte[]? _value;
        readonly TSeparator _separator;
        U8Range _current;
        U8Range _remaining;

        internal Enumerator(U8String value, TSeparator separator)
        {
            _value = value._value;
            _separator = separator;
            _remaining = value._inner;
        }

        public readonly U8String Current => new(_value, _current);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
        Next:
            var remaining = _remaining;
            if (remaining.Length > 0)
            {
                var value = _value!.SliceUnsafe(remaining.Offset, remaining.Length);
                var (index, length) = U8Searching.IndexOf(value, _separator);
                if (index >= 0)
                {
                    _current = TOptions.Trim
                        ? TrimEntry(_value!, new(remaining.Offset, index)) : new(remaining.Offset, index);
                    _remaining = new(
                        remaining.Offset + index + length,
                        remaining.Length - index - length);
                }
                else
                {
                    _current = TOptions.Trim
                        ? TrimEntry(_value!, remaining) : remaining;
                    _remaining = default;
                }

                if (TOptions.RemoveEmpty && _current.Length is 0)
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

public readonly struct ConfiguredU8Split<TSeparator, TOptions, TComparer> :
    IU8Enumerable<ConfiguredU8Split<TSeparator, TOptions, TComparer>.Enumerator>
        where TSeparator : struct
        where TOptions : unmanaged, IU8SplitOptions
        where TComparer : IU8ContainsOperator, IU8CountOperator, IU8IndexOfOperator
{
    readonly U8String _value;
    readonly TSeparator _separator;
    readonly TComparer _comparer;

    internal ConfiguredU8Split(U8String value, TSeparator separator, TComparer comparer)
    {
        _value = value;
        _separator = separator;
        _comparer = comparer;
    }

    public void Deconstruct(out U8String first, out U8String second)
    {
        this.Deconstruct<ConfiguredU8Split<TSeparator, TOptions, TComparer>, Enumerator, U8String>(out first, out second);
    }

    public void Deconstruct(out U8String first, out U8String second, out U8String third)
    {
        this.Deconstruct<ConfiguredU8Split<TSeparator, TOptions, TComparer>, Enumerator, U8String>(out first, out second, out third);
    }

    public U8String ElementAt(int index)
    {
        return this.ElementAt<ConfiguredU8Split<TSeparator, TOptions, TComparer>, Enumerator, U8String>(index);
    }

    public U8String ElementAtOrDefault(int index)
    {
        return this.ElementAtOrDefault<ConfiguredU8Split<TSeparator, TOptions, TComparer>, Enumerator, U8String>(index);
    }

    public U8String[] ToArray()
    {
        var hint = U8Searching.Count(_value, _separator, _comparer);
        return this.ToArrayUnsized<ConfiguredU8Split<TSeparator, TOptions, TComparer>, Enumerator, U8String>(hint);
    }

    public ImmutableArray<U8String> ToImmutableArray() => ImmutableCollectionsMarshal.AsImmutableArray(ToArray());

    public List<U8String> ToList()
    {
        var hint = U8Searching.Count(_value, _separator, _comparer);
        return this.ToListUnsized<ConfiguredU8Split<TSeparator, TOptions, TComparer>, Enumerator, U8String>(hint);
    }

    public Enumerator GetEnumerator() => new(_value, _separator, _comparer);

    IEnumerator<U8String> IEnumerable<U8String>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IU8Enumerator
    {
        readonly byte[]? _value;
        readonly TSeparator _separator;
        readonly TComparer _comparer;
        U8Range _current;
        U8Range _remaining;

        internal Enumerator(U8String value, TSeparator separator, TComparer comparer)
        {
            _value = value._value;
            _separator = separator;
            _comparer = comparer;
            _remaining = value._inner;
        }

        public readonly U8String Current => new(_value, _current);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
        Next:
            var remaining = _remaining;
            if (remaining.Length > 0)
            {
                var value = _value!.SliceUnsafe(remaining.Offset, remaining.Length);
                var (index, length) = U8Searching.IndexOf(value, _separator, _comparer);
                if (index >= 0)
                {
                    _current = TOptions.Trim
                        ? TrimEntry(_value!, new(remaining.Offset, index))
                        : new(remaining.Offset, index);
                    _remaining = new(
                        remaining.Offset + index + length,
                        remaining.Length - index - length);
                }
                else
                {
                    _current = TOptions.Trim
                        ? TrimEntry(_value!, remaining) : remaining;
                    _remaining = default;
                }

                if (TOptions.RemoveEmpty && _current.Length is 0)
                {
                    goto Next;
                }

                return true;
            }

            return false;
        }

        private static U8Range TrimEntry(byte[] value, U8Range range)
        {
            return new U8String(value, range).Trim()._inner;
        }

        readonly object IEnumerator.Current => Current;
        readonly void IEnumerator.Reset() => throw new NotSupportedException();
        readonly void IDisposable.Dispose() { }
    }
}

// Unfortunately, because ref structs cannot be used as generic type arguments,
// we have to resort to duplicating the implementation and exposing methods manually.
// What's worse, ConfiguredU8RefSplit has to be duplicated as well. Things we do for performance...
// TODO: Eventually, remove duplicate code once ref structs can be used as generics.
public readonly ref struct U8RefSplit
{
    readonly U8String _value;
    readonly ReadOnlySpan<byte> _separator;

    internal U8RefSplit(U8String value, ReadOnlySpan<byte> separator)
    {
        _value = value;
        _separator = separator;
    }

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var value = _value;
            var separator = _separator;

            return !value.IsEmpty
                ? U8Searching.Count(value.UnsafeSpan, separator) + 1 : 0;
        }
    }

    public bool Contains(U8String item)
    {
        return U8Searching.ContainsSegment(
            haystack: _value,
            needle: item,
            separator: _separator);
    }

    public bool Contains(ReadOnlySpan<byte> item)
    {
        return U8Searching.ContainsSegment(
            haystack: _value,
            needle: item,
            separator: _separator);
    }

    public void CopyTo(U8String[] array, int index)
    {
        var span = array.AsSpan()[index..];
        var split = this;
        if (split._value.Length > 0)
        {
            var count = split.Count;
            span = span[..count];

            var i = 0;
            ref var dst = ref span.AsRef();
            foreach (var item in split)
            {
                dst.Add(i++) = item;
            }
        }
    }

    public void Deconstruct(out U8String first, out U8String second)
    {
        (first, second) = (default, default);

        var enumerator = GetEnumerator();
        if (enumerator.MoveNext())
        {
            first = enumerator.Current;
            if (enumerator.MoveNext())
            {
                second = enumerator.Current;
            }
        }
    }

    public void Deconstruct(out U8String first, out U8String second, out U8String third)
    {
        (first, second, third) = (default, default, default);

        var enumerator = GetEnumerator();
        if (enumerator.MoveNext())
        {
            first = enumerator.Current;
            if (enumerator.MoveNext())
            {
                second = enumerator.Current;
                if (enumerator.MoveNext())
                {
                    third = enumerator.Current;
                }
            }
        }
    }

    public U8String ElementAt(int index)
    {
        if (index < 0)
        {
            ThrowHelpers.ArgumentOutOfRange<U8String>();
        }

        foreach (var item in this)
        {
            if (index-- is 0)
            {
                return item;
            }
        }

        return ThrowHelpers.ArgumentOutOfRange<U8String>();
    }

    public U8String ElementAtOrDefault(int index)
    {
        if (index < 0)
        {
            return default;
        }

        foreach (var item in this)
        {
            if (index-- is 0)
            {
                return item;
            }
        }

        return default;
    }

    public int FindOffset(U8String item)
    {
        return U8Searching.IndexOfSegment(
            haystack: _value,
            needle: item,
            separator: _separator);
    }

    public int FindOffset(ReadOnlySpan<byte> item)
    {
        return U8Searching.IndexOfSegment(
            haystack: _value,
            needle: item,
            separator: _separator);
    }

    public U8String[] ToArray()
    {
        var split = this;
        if (split._value.Length > 0)
        {
            var count = split.Count;
            var result = new U8String[count];
            var span = result.AsSpan();

            var i = 0;
            ref var dst = ref span.AsRef();
            foreach (var item in split)
            {
                dst.Add(i++) = item;
            }

            return result;
        }

        return U8Constants.EmptyStrings;
    }

    public ImmutableArray<U8String> ToImmutableArray() => ImmutableCollectionsMarshal.AsImmutableArray(ToArray());

    public List<U8String> ToList()
    {
        var split = this;
        if (split._value.Length > 0)
        {
            var count = split.Count;
            var result = new List<U8String>(count);
            CollectionsMarshal.SetCount(result, count);
            var span = CollectionsMarshal.AsSpan(result);

            var i = 0;
            ref var dst = ref span.AsRef();
            foreach (var item in split)
            {
                dst.Add(i++) = item;
            }

            return result;
        }

        return [];
    }

    public U8Slices ToSlices()
    {
        var split = this;
        var count = split.Count;
        if (count > 0)
        {
            var ranges = new U8Range[count];

            var i = 0;
            ref var dst = ref ranges.AsRef();
            foreach (var item in split)
            {
                dst.Add(i++) = item._inner;
            }

            return new U8Slices(split._value._value, ranges);
        }

        return default;
    }

    public readonly Enumerator GetEnumerator() => new(_value, _separator);

    public ref struct Enumerator
    {
        readonly byte[]? _value;
        readonly ReadOnlySpan<byte> _separator;
        U8Range _current;
        U8Range _remaining;

        internal Enumerator(U8String value, ReadOnlySpan<byte> separator)
        {
            _value = value._value;
            _separator = separator;
            _remaining = value._inner;
        }

        public readonly U8String Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_value, _current);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var remaining = _remaining;
            if (remaining.Length > 0)
            {
                var value = _value!.SliceUnsafe(remaining.Offset, remaining.Length);
                var separator = _separator;
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
    }
}

public readonly ref struct U8RefSplit<TComparer>
    where TComparer : IU8Comparer
{
    readonly U8String _value;
    readonly ReadOnlySpan<byte> _separator;
    readonly TComparer _comparer;

    internal U8RefSplit(U8String value, ReadOnlySpan<byte> separator, TComparer comparer)
    {
        _value = value;
        _separator = separator;
        _comparer = comparer;
    }

    public int Count
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var value = _value;
            var separator = _separator;
            var comparer = _comparer;

            if (!value.IsEmpty)
            {
                return U8Searching.Count(value.UnsafeSpan, separator, comparer) + 1;
            }

            return 0;
        }
    }

    public bool Contains(U8String item)
    {
        return U8Searching.SplitContains(
            haystack: _value,
            needle: item,
            separator: _separator,
            comparer: _comparer);
    }

    public bool Contains(ReadOnlySpan<byte> item)
    {
        return U8Searching.SplitContains(
            haystack: _value,
            needle: item,
            separator: _separator,
            comparer: _comparer);
    }

    public void CopyTo(U8String[] array, int index)
    {
        var span = array.AsSpan()[index..];
        var split = this;
        if (split._value.Length > 0)
        {
            var count = split.Count;
            span = span[..count];

            var i = 0;
            ref var dst = ref span.AsRef();
            foreach (var item in split)
            {
                dst.Add(i++) = item;
            }
        }
    }

    public void Deconstruct(out U8String first, out U8String second)
    {
        (first, second) = (default, default);

        var enumerator = GetEnumerator();
        if (enumerator.MoveNext())
        {
            first = enumerator.Current;
            if (enumerator.MoveNext())
            {
                second = enumerator.Current;
            }
        }
    }

    public void Deconstruct(out U8String first, out U8String second, out U8String third)
    {
        (first, second, third) = (default, default, default);

        var enumerator = GetEnumerator();
        if (enumerator.MoveNext())
        {
            first = enumerator.Current;
            if (enumerator.MoveNext())
            {
                second = enumerator.Current;
                if (enumerator.MoveNext())
                {
                    third = enumerator.Current;
                }
            }
        }
    }

    public U8String ElementAt(int index)
    {
        if (index < 0)
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        foreach (var item in this)
        {
            if (index-- is 0)
            {
                return item;
            }
        }

        return ThrowHelpers.ArgumentOutOfRange<U8String>();
    }

    public U8String ElementAtOrDefault(int index)
    {
        if (index < 0)
        {
            return default;
        }

        foreach (var item in this)
        {
            if (index-- is 0)
            {
                return item;
            }
        }

        return default;
    }

    public U8String[] ToArray()
    {
        var split = this;
        if (split._value.Length > 0)
        {
            var count = split.Count;
            var result = new U8String[count];
            var span = result.AsSpan();

            var i = 0;
            ref var dst = ref span.AsRef();
            foreach (var item in split)
            {
                dst.Add(i++) = item;
            }

            return result;
        }

        return U8Constants.EmptyStrings;
    }

    public ImmutableArray<U8String> ToImmutableArray() => ImmutableCollectionsMarshal.AsImmutableArray(ToArray());

    public List<U8String> ToList()
    {
        var split = this;
        if (split._value.Length > 0)
        {
            var count = split.Count;
            var result = new List<U8String>(count);
            CollectionsMarshal.SetCount(result, count);
            var span = CollectionsMarshal.AsSpan(result);

            var i = 0;
            ref var dst = ref span.AsRef();
            foreach (var item in split)
            {
                dst.Add(i++) = item;
            }

            return result;
        }

        return [];
    }

    public readonly Enumerator GetEnumerator() => new(_value, _separator, _comparer);

    public ref struct Enumerator
    {
        readonly byte[]? _value;
        readonly ReadOnlySpan<byte> _separator;
        readonly TComparer _comparer;
        U8Range _current;
        U8Range _remaining;

        internal Enumerator(U8String value, ReadOnlySpan<byte> separator, TComparer comparer)
        {
            _value = value._value;
            _separator = separator;
            _comparer = comparer;
            _remaining = value._inner;
        }

        public readonly U8String Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_value, _current);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var remaining = _remaining;
            if (remaining.Length > 0)
            {
                var value = _value!.SliceUnsafe(remaining.Offset, remaining.Length);
                var (index, length) = U8Searching.IndexOf(value, _separator, _comparer);
                if (index >= 0)
                {
                    _current = new(remaining.Offset, index);
                    _remaining = new(
                        remaining.Offset + index + length,
                        remaining.Length - index - length);
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
    }
}

public readonly ref struct ConfiguredU8RefSplit<TOptions>
    where TOptions : unmanaged, IU8SplitOptions
{
    readonly U8String _value;
    readonly ReadOnlySpan<byte> _separator;

    internal ConfiguredU8RefSplit(U8String value, ReadOnlySpan<byte> separator)
    {
        _value = value;
        _separator = separator;
    }

    public void Deconstruct(out U8String first, out U8String second)
    {
        (first, second) = (default, default);

        var enumerator = GetEnumerator();
        if (enumerator.MoveNext())
        {
            first = enumerator.Current;
            if (enumerator.MoveNext())
            {
                second = enumerator.Current;
            }
        }
    }

    public void Deconstruct(out U8String first, out U8String second, out U8String third)
    {
        (first, second, third) = (default, default, default);

        var enumerator = GetEnumerator();
        if (enumerator.MoveNext())
        {
            first = enumerator.Current;
            if (enumerator.MoveNext())
            {
                second = enumerator.Current;
                if (enumerator.MoveNext())
                {
                    third = enumerator.Current;
                }
            }
        }
    }

    public U8String ElementAt(int index)
    {
        if (index < 0)
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        foreach (var item in this)
        {
            if (index-- is 0)
            {
                return item;
            }
        }

        return ThrowHelpers.ArgumentOutOfRange<U8String>();
    }

    public U8String ElementAtOrDefault(int index)
    {
        if (index < 0)
        {
            return default;
        }

        foreach (var item in this)
        {
            if (index-- is 0)
            {
                return item;
            }
        }

        return default;
    }

    public U8String[] ToArray()
    {
        var split = this;
        if (split._value.Length > 0)
        {
            var count = U8Searching.Count(_value, _separator) + 1;
            var result = new U8String[count];
            var span = result.AsSpan();

            var i = 0;
            ref var dst = ref span.AsRef();
            foreach (var item in split)
            {
                dst.Add(i++) = item;
            }

            // TODO: This is a stopgap solution
            Array.Resize(ref result, i);
            return result;
        }

        return U8Constants.EmptyStrings;
    }

    public ImmutableArray<U8String> ToImmutableArray() => ImmutableCollectionsMarshal.AsImmutableArray(ToArray());

    public List<U8String> ToList()
    {
        var split = this;
        if (split._value.Length > 0)
        {
            var count = U8Searching.Count(_value, _separator) + 1;
            var result = new List<U8String>(count);
            CollectionsMarshal.SetCount(result, count);
            var span = CollectionsMarshal.AsSpan(result);

            var i = 0;
            ref var dst = ref span.AsRef();
            foreach (var item in split)
            {
                dst.Add(i++) = item;
            }

            CollectionsMarshal.SetCount(result, i);
            return result;
        }

        return [];
    }

    public readonly Enumerator GetEnumerator() => new(_value, _separator);

    public ref struct Enumerator
    {
        readonly byte[]? _value;
        readonly ReadOnlySpan<byte> _separator;
        U8Range _current;
        U8Range _remaining;

        internal Enumerator(U8String value, ReadOnlySpan<byte> separator)
        {
            _value = value._value;
            _separator = separator;
            _remaining = value._inner;
        }

        public readonly U8String Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_value, _current);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
        Next:
            var remaining = _remaining;
            if (remaining.Length > 0)
            {
                var value = _value!.SliceUnsafe(remaining.Offset, remaining.Length);
                var separator = _separator;
                var index = U8Searching.IndexOf(value, separator);
                if (index >= 0)
                {
                    _current = TOptions.Trim
                        ? TrimEntry(_value!, new(remaining.Offset, index))
                        : new(remaining.Offset, index);
                    _remaining = new(
                        remaining.Offset + index + separator.Length,
                        remaining.Length - index - separator.Length);
                }
                else
                {
                    _current = TOptions.Trim
                        ? TrimEntry(_value!, remaining) : remaining;
                    _remaining = default;
                }

                if (TOptions.RemoveEmpty && _current.Length is 0)
                {
                    goto Next;
                }

                return true;
            }

            return false;
        }

        private static U8Range TrimEntry(byte[] value, U8Range range)
        {
            return new U8String(value, range).Trim()._inner;
        }
    }
}

public readonly ref struct ConfiguredU8RefSplit<TOptions, TComparer>
    where TOptions : unmanaged, IU8SplitOptions
    where TComparer : IU8ContainsOperator, IU8CountOperator, IU8IndexOfOperator
{
    readonly U8String _value;
    readonly ReadOnlySpan<byte> _separator;
    readonly TComparer _comparer;

    internal ConfiguredU8RefSplit(U8String value, ReadOnlySpan<byte> separator, TComparer comparer)
    {
        _value = value;
        _separator = separator;
        _comparer = comparer;
    }

    public void Deconstruct(out U8String first, out U8String second)
    {
        (first, second) = (default, default);

        var enumerator = GetEnumerator();
        if (enumerator.MoveNext())
        {
            first = enumerator.Current;
            if (enumerator.MoveNext())
            {
                second = enumerator.Current;
            }
        }
    }

    public void Deconstruct(out U8String first, out U8String second, out U8String third)
    {
        (first, second, third) = (default, default, default);

        var enumerator = GetEnumerator();
        if (enumerator.MoveNext())
        {
            first = enumerator.Current;
            if (enumerator.MoveNext())
            {
                second = enumerator.Current;
                if (enumerator.MoveNext())
                {
                    third = enumerator.Current;
                }
            }
        }
    }

    public U8String ElementAt(int index)
    {
        if (index < 0)
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        foreach (var item in this)
        {
            if (index-- is 0)
            {
                return item;
            }
        }

        return ThrowHelpers.ArgumentOutOfRange<U8String>();
    }

    public U8String ElementAtOrDefault(int index)
    {
        if (index < 0)
        {
            return default;
        }

        foreach (var item in this)
        {
            if (index-- is 0)
            {
                return item;
            }
        }

        return default;
    }

    public U8String[] ToArray()
    {
        var split = this;
        if (split._value.Length > 0)
        {
            var count = U8Searching.Count(_value.UnsafeSpan, _separator, _comparer) + 1;
            var result = new U8String[count];
            var span = result.AsSpan();

            var i = 0;
            ref var dst = ref span.AsRef();
            foreach (var item in split)
            {
                dst.Add(i++) = item;
            }

            Array.Resize(ref result, i);
            return result;
        }

        return U8Constants.EmptyStrings;
    }

    public ImmutableArray<U8String> ToImmutableArray() => ImmutableCollectionsMarshal.AsImmutableArray(ToArray());

    public List<U8String> ToList()
    {
        var split = this;
        if (split._value.Length > 0)
        {
            var count = U8Searching.Count(_value.UnsafeSpan, _separator, _comparer) + 1;
            var result = new List<U8String>(count);
            CollectionsMarshal.SetCount(result, count);
            var span = CollectionsMarshal.AsSpan(result);

            var i = 0;
            ref var dst = ref span.AsRef();
            foreach (var item in split)
            {
                dst.Add(i++) = item;
            }

            CollectionsMarshal.SetCount(result, i);
            return result;
        }

        return [];
    }

    public readonly Enumerator GetEnumerator() => new(_value, _separator, _comparer);

    public ref struct Enumerator
    {
        readonly byte[]? _value;
        readonly ReadOnlySpan<byte> _separator;
        readonly TComparer _comparer;
        U8Range _current;
        U8Range _remaining;

        internal Enumerator(U8String value, ReadOnlySpan<byte> separator, TComparer comparer)
        {
            _value = value._value;
            _separator = separator;
            _comparer = comparer;
            _remaining = value._inner;
        }

        public readonly U8String Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_value, _current);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
        Next:
            var remaining = _remaining;
            if (remaining.Length > 0)
            {
                var value = _value!.SliceUnsafe(remaining.Offset, remaining.Length);
                var (index, length) = U8Searching.IndexOf(value, _separator, _comparer);
                if (index >= 0)
                {
                    _current = TOptions.Trim
                        ? TrimEntry(_value!, new(remaining.Offset, index)) : new(remaining.Offset, index);
                    _remaining = new(
                        remaining.Offset + index + length,
                        remaining.Length - index - length);
                }
                else
                {
                    _current = TOptions.Trim
                        ? TrimEntry(_value!, remaining) : remaining;
                    _remaining = default;
                }

                if (TOptions.RemoveEmpty && _current.Length is 0)
                {
                    goto Next;
                }

                return true;
            }

            return false;
        }

        private static U8Range TrimEntry(byte[] value, U8Range range)
        {
            return new U8String(value, range).Trim()._inner;
        }
    }
}

public readonly ref struct U8RefAnySplit
{
    readonly U8String _value;
    readonly ReadOnlySpan<byte> _separators;

    internal U8RefAnySplit(U8String value, ReadOnlySpan<byte> separators)
    {
        _value = value;
        _separators = separators;
    }

    public Enumerator GetEnumerator() => new(_value, _separators);

    public ref struct Enumerator
    {
        readonly byte[]? _value;
        readonly ReadOnlySpan<byte> _separators;
        U8Range _current;
        U8Range _remaining;

        internal Enumerator(U8String value, ReadOnlySpan<byte> separators)
        {
            _value = value._value;
            _separators = separators;
            _remaining = value._inner;
        }

        public readonly U8String Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_value, _current);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var remaining = _remaining;
            if (remaining.Length > 0)
            {
                var value = _value!.SliceUnsafe(remaining.Offset, remaining.Length);
                var index = value.IndexOfAny(_separators);
                if (index >= 0)
                {
                    _current = new(remaining.Offset, index);
                    _remaining = new(
                        remaining.Offset + index + 1,
                        remaining.Length - index - 1);
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
    }
}
