using System.Collections;
using System.Runtime.InteropServices;

using U8Primitives.Abstractions;
using U8Primitives.InteropServices;

namespace U8Primitives;

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

public readonly struct U8Split(U8String value, U8String separator) :
    ICollection<U8String>,
    IU8Enumerable<U8Split.Enumerator>
{
    readonly U8String _value = value;
    readonly U8String _separator = separator;

    public int Count
    {
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

    public bool Contains(U8String item)
    {
        return U8Searching.SplitContains(_value, _separator, item);
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

    public U8String[] ToArray() => this.ToArray<U8Split, Enumerator, U8String>();
    public List<U8String> ToList() => this.ToList<U8Split, Enumerator, U8String>();

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

        public readonly U8String Current => new(_value, _current.Offset, _current.Length);

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

// TODO: Optimize even more. This design is far from the northstar of perfect codegen
// but it still somehow manages to outperform Rust split iterators
public readonly struct U8Split<TSeparator> :
    ICollection<U8String>,
    IU8Enumerable<U8Split<TSeparator>.Enumerator>
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
        get
        {
            var value = _value;
            return !value.IsEmpty
                ? U8Searching.Count(value.UnsafeSpan, _separator) + 1 : 0;
        }
    }

    public bool Contains(U8String item)
    {
        return U8Searching.SplitContains(_value, _separator, item);
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

    public U8String[] ToArray() => this.ToArray<U8Split<TSeparator>, Enumerator, U8String>();
    public List<U8String> ToList() => this.ToList<U8Split<TSeparator>, Enumerator, U8String>();

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

        public readonly U8String Current => new(_value, _current.Offset, _current.Length);

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
    ICollection<U8String>,
    IU8Enumerable<U8Split<TSeparator, TComparer>.Enumerator>
        where TSeparator : struct
        where TComparer : IU8ContainsOperator, IU8CountOperator, IU8IndexOfOperator
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
        get
        {
            var value = _value;
            return !value.IsEmpty
                ? U8Searching.Count(value.UnsafeSpan, _separator, _comparer) + 1 : 0;
        }
    }

    public bool Contains(U8String item)
    {
        return U8Searching.SplitContains(_value, _separator, item, _comparer);
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
        public readonly U8String Current => new(_value, _current.Offset, _current.Length);

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

[Flags]
public enum U8SplitOptions : byte
{
    None = 0,
    RemoveEmpty = 1,
    Trim = 2,
}

public readonly struct ConfiguredU8Split :
    IU8Enumerable<ConfiguredU8Split.Enumerator>
{
    readonly U8String _value;
    readonly U8String _separator;
    readonly U8SplitOptions _options;

    public ConfiguredU8Split(U8String value, U8String separator, U8SplitOptions options)
    {
        _value = value;
        _separator = separator;
        _options = options;
    }

    public void Deconstruct(out U8String first, out U8String second)
    {
        this.Deconstruct<ConfiguredU8Split, Enumerator, U8String>(out first, out second);
    }

    public void Deconstruct(out U8String first, out U8String second, out U8String third)
    {
        this.Deconstruct<ConfiguredU8Split, Enumerator, U8String>(out first, out second, out third);
    }

    public U8String ElementAt(int index)
    {
        return this.ElementAt<ConfiguredU8Split, Enumerator, U8String>(index);
    }

    public U8String ElementAtOrDefault(int index)
    {
        return this.ElementAtOrDefault<ConfiguredU8Split, Enumerator, U8String>(index);
    }

    public U8String[] ToArray()
    {
        var hint = U8Searching.Count(_value, _separator);
        return this.ToArrayUnsized<ConfiguredU8Split, Enumerator, U8String>(hint);
    }

    public List<U8String> ToList()
    {
        var hint = U8Searching.Count(_value, _separator);
        return this.ToListUnsized<ConfiguredU8Split, Enumerator, U8String>(hint);
    }

    public Enumerator GetEnumerator() => new(_value, _separator, _options);

    IEnumerator<U8String> IEnumerable<U8String>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IU8Enumerator
    {
        readonly byte[]? _value;
        readonly U8String _separator;
        readonly U8SplitOptions _options;
        U8Range _current;
        U8Range _remaining;

        public Enumerator(U8String value, U8String separator, U8SplitOptions options)
        {
            _value = value._value;
            _separator = separator;
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
                var value = _value!.SliceUnsafe(remaining.Offset, remaining.Length);
                var separator = _separator;
                var index = value.IndexOf(separator);
                if (index >= 0)
                {
                    _current = (_options & U8SplitOptions.Trim) != U8SplitOptions.Trim
                        ? new(remaining.Offset, index)
                        : TrimEntry(_value!, new(remaining.Offset, index));
                    _remaining = new(
                        remaining.Offset + index + separator.Length,
                        remaining.Length - index - separator.Length);
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

public readonly struct ConfiguredU8Split<TSeparator> :
    IU8Enumerable<ConfiguredU8Split<TSeparator>.Enumerator>
        where TSeparator : unmanaged
{
    readonly U8String _value;
    readonly TSeparator _separator;
    readonly U8SplitOptions _options;

    internal ConfiguredU8Split(U8String value, TSeparator separator, U8SplitOptions options)
    {
        _value = value;
        _separator = separator;
        _options = options;
    }

    public void Deconstruct(out U8String first, out U8String second)
    {
        this.Deconstruct<ConfiguredU8Split<TSeparator>, Enumerator, U8String>(out first, out second);
    }

    public void Deconstruct(out U8String first, out U8String second, out U8String third)
    {
        this.Deconstruct<ConfiguredU8Split<TSeparator>, Enumerator, U8String>(out first, out second, out third);
    }

    public U8String ElementAt(int index)
    {
        return this.ElementAt<ConfiguredU8Split<TSeparator>, Enumerator, U8String>(index);
    }

    public U8String ElementAtOrDefault(int index)
    {
        return this.ElementAtOrDefault<ConfiguredU8Split<TSeparator>, Enumerator, U8String>(index);
    }

    public U8String[] ToArray()
    {
        var hint = U8Searching.Count(_value, _separator);
        return this.ToArrayUnsized<ConfiguredU8Split<TSeparator>, Enumerator, U8String>(hint);
    }

    public List<U8String> ToList()
    {
        var hint = U8Searching.Count(_value, _separator);
        return this.ToListUnsized<ConfiguredU8Split<TSeparator>, Enumerator, U8String>(hint);
    }

    public Enumerator GetEnumerator() => new(_value, _separator, _options);

    IEnumerator<U8String> IEnumerable<U8String>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IU8Enumerator
    {
        readonly byte[]? _value;
        readonly TSeparator _separator;
        readonly U8SplitOptions _options;
        U8Range _current;
        U8Range _remaining;

        internal Enumerator(U8String value, TSeparator separator, U8SplitOptions options)
        {
            _value = value._value;
            _separator = separator;
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
                var value = _value!.SliceUnsafe(remaining.Offset, remaining.Length);
                var (index, length) = U8Searching.IndexOf(value, _separator);
                if (index >= 0)
                {
                    _current = (_options & U8SplitOptions.Trim) != U8SplitOptions.Trim
                        ? new(remaining.Offset, index)
                        : TrimEntry(_value!, new(remaining.Offset, index));
                    _remaining = new(
                        remaining.Offset + index + length,
                        remaining.Length - index - length);
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

public readonly struct ConfiguredU8Split<TSeparator, TComparer> :
    IU8Enumerable<ConfiguredU8Split<TSeparator, TComparer>.Enumerator>
        where TSeparator : struct
        where TComparer : IU8ContainsOperator, IU8CountOperator, IU8IndexOfOperator
{
    readonly U8String _value;
    readonly TSeparator _separator;
    readonly TComparer _comparer;
    readonly U8SplitOptions _options;

    internal ConfiguredU8Split(U8String value, TSeparator separator, TComparer comparer, U8SplitOptions options)
    {
        _value = value;
        _separator = separator;
        _comparer = comparer;
        _options = options;
    }

    public void Deconstruct(out U8String first, out U8String second)
    {
        this.Deconstruct<ConfiguredU8Split<TSeparator, TComparer>, Enumerator, U8String>(out first, out second);
    }

    public void Deconstruct(out U8String first, out U8String second, out U8String third)
    {
        this.Deconstruct<ConfiguredU8Split<TSeparator, TComparer>, Enumerator, U8String>(out first, out second, out third);
    }

    public U8String ElementAt(int index)
    {
        return this.ElementAt<ConfiguredU8Split<TSeparator, TComparer>, Enumerator, U8String>(index);
    }

    public U8String ElementAtOrDefault(int index)
    {
        return this.ElementAtOrDefault<ConfiguredU8Split<TSeparator, TComparer>, Enumerator, U8String>(index);
    }

    public U8String[] ToArray()
    {
        var hint = U8Searching.Count(_value, _separator, _comparer);
        return this.ToArrayUnsized<ConfiguredU8Split<TSeparator, TComparer>, Enumerator, U8String>(hint);
    }

    public List<U8String> ToList()
    {
        var hint = U8Searching.Count(_value, _separator, _comparer);
        return this.ToListUnsized<ConfiguredU8Split<TSeparator, TComparer>, Enumerator, U8String>(hint);
    }

    public Enumerator GetEnumerator() => new(_value, _separator, _comparer, _options);

    IEnumerator<U8String> IEnumerable<U8String>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator : IU8Enumerator
    {
        readonly byte[]? _value;
        readonly TSeparator _separator;
        readonly TComparer _comparer;
        readonly U8SplitOptions _options;
        U8Range _current;
        U8Range _remaining;

        internal Enumerator(U8String value, TSeparator separator, TComparer comparer, U8SplitOptions options)
        {
            _value = value._value;
            _separator = separator;
            _comparer = comparer;
            _options = options;
            _remaining = value._inner;
        }

        public readonly U8String Current => new(_value, _current.Offset, _current.Length);

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
                    _current = (_options & U8SplitOptions.Trim) != U8SplitOptions.Trim
                        ? new(remaining.Offset, index)
                        : TrimEntry(_value!, new(remaining.Offset, index));
                    _remaining = new(
                        remaining.Offset + index + length,
                        remaining.Length - index - length);
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
        get
        {
            var split = this;
            if (!split._value.IsEmpty)
            {
                return U8Searching.Count(
                    split._value.UnsafeSpan,
                    split._separator) + 1;
            }

            return 0;
        }
    }

    public bool Contains(U8String item)
    {
        return U8Searching.SplitContains(_value, _separator, item);
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

        return [];
    }

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
            get => new(_value, _current.Offset, _current.Length);
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
    where TComparer : IU8ContainsOperator, IU8CountOperator, IU8IndexOfOperator
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
        get
        {
            var split = this;
            if (!split._value.IsEmpty)
            {
                return U8Searching.Count(
                    split._value.UnsafeSpan,
                    split._separator,
                    split._comparer) + 1;
            }

            return 0;
        }
    }

    public bool Contains(U8String item)
    {
        return U8Searching.SplitContains(_value, _separator, item, _comparer);
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

        return [];
    }

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
            get => new(_value, _current.Offset, _current.Length);
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

public readonly ref struct ConfiguredU8RefSplit
{
    readonly U8String _value;
    readonly ReadOnlySpan<byte> _separator;
    readonly U8SplitOptions _options;

    internal ConfiguredU8RefSplit(U8String value, ReadOnlySpan<byte> separator, U8SplitOptions options)
    {
        _value = value;
        _separator = separator;
        _options = options;
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

        return [];
    }

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

    public readonly Enumerator GetEnumerator() => new(_value, _separator, _options);

    public ref struct Enumerator
    {
        readonly byte[]? _value;
        readonly ReadOnlySpan<byte> _separator;
        readonly U8SplitOptions _options;
        U8Range _current;
        U8Range _remaining;

        internal Enumerator(U8String value, ReadOnlySpan<byte> separator, U8SplitOptions options)
        {
            _value = value._value;
            _separator = separator;
            _options = options;
            _remaining = value._inner;
        }

        public readonly U8String Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_value, _current.Offset, _current.Length);
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
                    _current = (_options & U8SplitOptions.Trim) != U8SplitOptions.Trim
                        ? new(remaining.Offset, index)
                        : TrimEntry(_value!, new(remaining.Offset, index));
                    _remaining = new(
                        remaining.Offset + index + separator.Length,
                        remaining.Length - index - separator.Length);
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
            return new U8String(value, range).Trim()._inner;
        }
    }
}

public readonly ref struct ConfiguredU8RefSplit<TComparer>
    where TComparer : IU8ContainsOperator, IU8CountOperator, IU8IndexOfOperator
{
    readonly U8String _value;
    readonly ReadOnlySpan<byte> _separator;
    readonly TComparer _comparer;
    readonly U8SplitOptions _options;

    internal ConfiguredU8RefSplit(U8String value, ReadOnlySpan<byte> separator, TComparer comparer, U8SplitOptions options)
    {
        _value = value;
        _separator = separator;
        _comparer = comparer;
        _options = options;
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

        return [];
    }

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

    public readonly Enumerator GetEnumerator() => new(_value, _separator, _comparer, _options);

    public ref struct Enumerator
    {
        readonly byte[]? _value;
        readonly ReadOnlySpan<byte> _separator;
        readonly TComparer _comparer;
        readonly U8SplitOptions _options;
        U8Range _current;
        U8Range _remaining;

        internal Enumerator(U8String value, ReadOnlySpan<byte> separator, TComparer comparer, U8SplitOptions options)
        {
            _value = value._value;
            _separator = separator;
            _comparer = comparer;
            _options = options;
            _remaining = value._inner;
        }

        public readonly U8String Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_value, _current.Offset, _current.Length);
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
                    _current = (_options & U8SplitOptions.Trim) != U8SplitOptions.Trim
                        ? new(remaining.Offset, index)
                        : TrimEntry(_value!, new(remaining.Offset, index));
                    _remaining = new(
                        remaining.Offset + index + length,
                        remaining.Length - index - length);
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
            _remaining = value.Range;
        }

        public readonly U8String Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(_value, _current.Offset, _current.Length);
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
