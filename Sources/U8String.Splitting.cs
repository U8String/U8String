using System.Text;

using U8Primitives.Abstractions;

#pragma warning disable RCS1085, RCS1085FadeOut, IDE0032 // Use auto-implemented property. Why: readable fields.
namespace U8Primitives;

public readonly partial struct U8String
{
    // TODO: Dedup SplitFirst/Last
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

    public U8SplitPair SplitFirst(char separator)
    {
        ThrowHelpers.CheckSurrogate(separator);

        return char.IsAscii(separator)
            ? SplitFirst((byte)separator)
            : SplitFirstUnchecked(U8Scalar.Create(separator, checkAscii: false).AsSpan());
    }

    public U8SplitPair SplitFirst(Rune separator) => separator.IsAscii
        ? SplitFirst((byte)separator.Value)
        : SplitFirstUnchecked(U8Scalar.Create(separator, checkAscii: false).AsSpan());

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

    internal U8SplitPair SplitFirstUnchecked(ReadOnlySpan<byte> separator)
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

    public U8SplitPair SplitFirst<T>(byte separator, T comparer)
        where T : IU8IndexOfOperator
    {
        if (!U8Info.IsAsciiByte(separator))
        {
            // TODO: EH UX
            ThrowHelpers.ArgumentOutOfRange();
        }

        var source = this;
        if (!source.IsEmpty)
        {
            var (index, stride) = comparer.IndexOf(source.UnsafeSpan, separator);

            if (index >= 0)
            {
                return new(source, index, stride);
            }

            return U8SplitPair.NotFound(source);
        }

        return default;
    }

    public U8SplitPair SplitFirst<T>(char separator, T comparer)
        where T : IU8IndexOfOperator
    {
        return char.IsAscii(separator)
            ? SplitFirst((byte)separator, comparer)
            : SplitFirstUnchecked(U8Scalar.Create(separator, checkAscii: false).AsSpan(), comparer);
    }

    public U8SplitPair SplitFirst<T>(Rune separator, T comparer)
        where T : IU8IndexOfOperator
    {
        return separator.IsAscii
            ? SplitFirst((byte)separator.Value, comparer)
            : SplitFirstUnchecked(U8Scalar.Create(separator, checkAscii: false).AsSpan(), comparer);
    }

    internal U8SplitPair SplitFirst<T>(U8String separator, T comparer)
        where T : IU8IndexOfOperator
    {
        var source = this;
        if (!source.IsEmpty)
        {
            if (!separator.IsEmpty)
            {
                var (index, stride) = U8Searching.IndexOf(source.UnsafeSpan, separator.UnsafeSpan, comparer);

                if (index >= 0)
                {
                    return new(source, index, stride);
                }
            }

            return U8SplitPair.NotFound(source);
        }

        return default;
    }

    public U8SplitPair SplitFirst<T>(ReadOnlySpan<byte> separator, T comparer)
        where T : IU8IndexOfOperator
    {
        var source = this;
        if (!source.IsEmpty)
        {
            if (!separator.IsEmpty)
            {
                var (index, stride) = U8Searching.IndexOf(source.UnsafeSpan, separator, comparer);

                if (index >= 0)
                {
                    // Same as with Slice(int, int), this might dereference past the end of the string.
                    // TODO: Do something about it if it's ever an issue.
                    if (U8Info.IsContinuationByte(source.UnsafeRefAdd(index)) ||
                        U8Info.IsContinuationByte(source.UnsafeRefAdd(index + stride)))
                    {
                        ThrowHelpers.InvalidSplit();
                    }

                    return new(source, index, stride);
                }
            }

            return U8SplitPair.NotFound(source);
        }

        return default;
    }

    internal U8SplitPair SplitFirstUnchecked<T>(ReadOnlySpan<byte> separator, T comparer)
        where T : IU8IndexOfOperator
    {
        var source = this;
        if (!source.IsEmpty)
        {
            if (!separator.IsEmpty)
            {
                var (index, stride) = U8Searching.IndexOf(source.UnsafeSpan, separator, comparer);

                if (index >= 0)
                {
                    return new(source, index, stride);
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

    public U8SplitPair SplitLast(char separator)
    {
        ThrowHelpers.CheckSurrogate(separator);

        return char.IsAscii(separator)
            ? SplitLast((byte)separator)
            : SplitLastUnchecked(U8Scalar.Create(separator, checkAscii: false).AsSpan());
    }

    public U8SplitPair SplitLast(Rune separator) => separator.IsAscii
        ? SplitLast((byte)separator.Value)
        : SplitLastUnchecked(U8Scalar.Create(separator, checkAscii: false).AsSpan());

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

    public U8SplitPair SplitLast<T>(byte separator, T comparer)
        where T : IU8LastIndexOfOperator
    {
        if (!U8Info.IsAsciiByte(separator))
        {
            // TODO: EH UX
            ThrowHelpers.ArgumentOutOfRange();
        }

        var source = this;
        if (!source.IsEmpty)
        {
            var (index, stride) = comparer.LastIndexOf(source.UnsafeSpan, separator);

            if (index >= 0)
            {
                return new(source, index, stride);
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

    public U8Split<char> Split(char separator)
    {
        ThrowHelpers.CheckSurrogate(separator);

        return new(this, separator);
    }

    public U8Split<Rune> Split(Rune separator) => new(this, separator);

    public U8Split Split(U8String separator)
    {
        return new(this, separator);
    }

    public U8RefSplit Split(ReadOnlySpan<byte> separator)
    {
        if (!IsValid(separator))
        {
            ThrowHelpers.InvalidSplit();
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

    public ConfiguredU8Split<char> Split(char separator, U8SplitOptions options)
    {
        ThrowHelpers.CheckSurrogate(separator);

        return new(this, separator, options);
    }

    public ConfiguredU8Split<Rune> Split(Rune separator, U8SplitOptions options) => new(this, separator, options);

    public ConfiguredU8Split Split(U8String separator, U8SplitOptions options)
    {
        return new(this, separator, options);
    }

    // TODO: Consider aggregating multiple interfaces into a single IU8Searcher (better name???) interface.
    public U8Split<byte, T> Split<T>(byte separator, T comparer)
        where T : IU8ContainsOperator, IU8CountOperator, IU8IndexOfOperator
    {
        if (!U8Info.IsAsciiByte(separator))
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        return new(this, separator, comparer);
    }

    public U8Split<char, T> Split<T>(char separator, T comparer)
        where T : IU8ContainsOperator, IU8CountOperator, IU8IndexOfOperator
    {
        ThrowHelpers.CheckSurrogate(separator);

        return new(this, separator, comparer);
    }

    public U8Split<Rune, T> Split<T>(Rune separator, T comparer)
        where T : IU8ContainsOperator, IU8CountOperator, IU8IndexOfOperator
    {
        return new(this, separator, comparer);
    }

    public U8Split<U8String, T> Split<T>(U8String separator, T comparer)
        where T : IU8ContainsOperator, IU8CountOperator, IU8IndexOfOperator
    {
        return new(this, separator, comparer);
    }

    public U8RefSplit<T> Split<T>(ReadOnlySpan<byte> separator, T comparer)
        where T : IU8ContainsOperator, IU8CountOperator, IU8IndexOfOperator
    {
        if (!IsValid(separator))
        {
            ThrowHelpers.InvalidSplit();
        }

        return new(this, separator, comparer);
    }

    public ConfiguredU8Split<byte, T> Split<T>(byte separator, T comparer, U8SplitOptions options)
        where T : IU8ContainsOperator, IU8CountOperator, IU8IndexOfOperator
    {
        if (!U8Info.IsAsciiByte(separator))
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        return new(this, separator, comparer, options);
    }

    public ConfiguredU8Split<char, T> Split<T>(char separator, T comparer, U8SplitOptions options)
        where T : IU8ContainsOperator, IU8CountOperator, IU8IndexOfOperator
    {
        ThrowHelpers.CheckSurrogate(separator);

        return new(this, separator, comparer, options);
    }

    public ConfiguredU8Split<Rune, T> Split<T>(Rune separator, T comparer, U8SplitOptions options)
        where T : IU8ContainsOperator, IU8CountOperator, IU8IndexOfOperator
    {
        return new(this, separator, comparer, options);
    }

    public ConfiguredU8Split<U8String, T> Split<T>(U8String separator, T comparer, U8SplitOptions options)
        where T : IU8ContainsOperator, IU8CountOperator, IU8IndexOfOperator
    {
        return new(this, separator, comparer, options);
    }

    public ConfiguredU8RefSplit<T> Split<T>(ReadOnlySpan<byte> separator, T comparer, U8SplitOptions options)
        where T : IU8ContainsOperator, IU8CountOperator, IU8IndexOfOperator
    {
        if (!IsValid(separator))
        {
            ThrowHelpers.InvalidSplit();
        }

        return new(this, separator, comparer, options);
    }

    public U8RefAnySplit SplitAny(ReadOnlySpan<byte> separators)
    {
        if (!IsValid(separators))
        {
            ThrowHelpers.InvalidSplit();
        }

        return new(this, separators);
    }
}
