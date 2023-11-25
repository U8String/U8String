using System.Diagnostics;
using System.Text;

using U8Primitives.Abstractions;

#pragma warning disable RCS1085, RCS1085FadeOut, IDE0032 // Use auto-implemented property. Why: readable fields.
namespace U8Primitives;

public readonly partial struct U8String
{
    // TODO: Dedup SplitFirst/Last
    public U8SplitPair SplitFirst(byte separator)
    {
        ThrowHelpers.CheckAscii(separator);

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

        if (char.IsAscii(separator))
        {
            return SplitFirst((byte)separator);
        }

        return SplitFirstUnchecked(new U8Scalar(separator, checkAscii: false).AsSpan());
    }

    public U8SplitPair SplitFirst(Rune separator)
    {
        if (separator.IsAscii)
        {
            return SplitFirst((byte)separator.Value);
        }

        return SplitFirstUnchecked(new U8Scalar(separator, checkAscii: false).AsSpan());
    }

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

    public U8SplitPair SplitFirst(ReadOnlySpan<byte> separator)
    {
        var source = this;
        if (!source.IsEmpty)
        {
            if (separator.Length > 0)
            {
                var index = source.UnsafeSpan.IndexOf(separator);
                if (index >= 0)
                {
                    var end = index + separator.Length;
                    if (U8Info.IsContinuationByte(source.UnsafeRefAdd(index)) || (end < source.Length &&
                        U8Info.IsContinuationByte(source.UnsafeRefAdd(end))))
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

    U8SplitPair SplitFirstUnchecked(ReadOnlySpan<byte> separator)
    {
        Debug.Assert(separator.Length > 0);

        var source = this;
        if (!source.IsEmpty)
        {
            var span = source.UnsafeSpan;
            var index = span.IndexOf(separator);
            if (index >= 0)
            {
                return new(source, index, separator.Length);
            }

            return U8SplitPair.NotFound(source);
        }

        return default;
    }

    public U8SplitPair SplitFirst<T>(byte separator, T comparer)
        where T : IU8IndexOfOperator
    {
        ThrowHelpers.CheckAscii(separator);

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
        ThrowHelpers.CheckSurrogate(separator);

        if (char.IsAscii(separator))
        {
            return SplitFirst((byte)separator, comparer);
        }

        return SplitFirstUnchecked(new U8Scalar(separator, checkAscii: false).AsSpan(), comparer);
    }

    public U8SplitPair SplitFirst<T>(Rune separator, T comparer)
        where T : IU8IndexOfOperator
    {
        if (separator.IsAscii)
        {
            return SplitFirst((byte)separator.Value, comparer);
        }

        return SplitFirstUnchecked(new U8Scalar(separator, checkAscii: false).AsSpan(), comparer);
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
            if (separator.Length > 0)
            {
                var (index, stride) = U8Searching.IndexOf(source.UnsafeSpan, separator, comparer);

                if (index >= 0)
                {
                    // Same as with Slice(int, int), this might dereference past the end of the string.
                    // TODO: Do something about it if it's ever an issue.
                    var end = index + stride;
                    if (U8Info.IsContinuationByte(source.UnsafeRefAdd(index)) || (end < source.Length &&
                        U8Info.IsContinuationByte(source.UnsafeRefAdd(end))))
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

    U8SplitPair SplitFirstUnchecked<T>(ReadOnlySpan<byte> separator, T comparer)
        where T : IU8IndexOfOperator
    {
        Debug.Assert(separator.Length > 0);

        var source = this;
        if (!source.IsEmpty)
        {
            var (index, stride) = U8Searching.IndexOf(source.UnsafeSpan, separator, comparer);

            if (index >= 0)
            {
                return new(source, index, stride);
            }

            return U8SplitPair.NotFound(source);
        }

        return default;
    }

    public U8SplitPair SplitLast(byte separator)
    {
        ThrowHelpers.CheckAscii(separator);

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

        if (char.IsAscii(separator))
        {
            return SplitLast((byte)separator);
        }

        return SplitLastUnchecked(new U8Scalar(separator, checkAscii: false).AsSpan());
    }

    public U8SplitPair SplitLast(Rune separator)
    {
        if (separator.IsAscii)
        {
            return SplitLast((byte)separator.Value);
        }

        return SplitLastUnchecked(new U8Scalar(separator, checkAscii: false).AsSpan());
    }

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
            if (separator.Length > 0)
            {
                var index = source.UnsafeSpan.LastIndexOf(separator);
                if (index >= 0)
                {
                    var end = index + separator.Length;
                    if (U8Info.IsContinuationByte(source.UnsafeRefAdd(index)) || (end < source.Length &&
                        U8Info.IsContinuationByte(source.UnsafeRefAdd(end))))
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

    internal U8SplitPair SplitLastUnchecked(ReadOnlySpan<byte> separator)
    {
        Debug.Assert(separator.Length > 0);

        var source = this;
        if (!source.IsEmpty)
        {
            var span = source.UnsafeSpan;
            var index = span.LastIndexOf(separator);
            if (index >= 0)
            {
                return new(source, index, separator.Length);
            }

            return U8SplitPair.NotFound(source);
        }

        return default;
    }

    public U8SplitPair SplitLast<T>(byte separator, T comparer)
        where T : IU8LastIndexOfOperator
    {
        ThrowHelpers.CheckAscii(separator);

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

    public U8SplitPair SplitLast<T>(char separator, T comparer)
        where T : IU8LastIndexOfOperator
    {
        ThrowHelpers.CheckSurrogate(separator);

        if (char.IsAscii(separator))
        {
            return SplitLast((byte)separator, comparer);
        }

        return SplitLastUnchecked(new U8Scalar(separator, checkAscii: false).AsSpan(), comparer);
    }

    public U8SplitPair SplitLast<T>(Rune separator, T comparer)
        where T : IU8LastIndexOfOperator
    {
        if (separator.IsAscii)
        {
            return SplitLast((byte)separator.Value, comparer);
        }

        return SplitLastUnchecked(new U8Scalar(separator, checkAscii: false).AsSpan(), comparer);
    }

    internal U8SplitPair SplitLast<T>(U8String separator, T comparer)
        where T : IU8LastIndexOfOperator
    {
        var source = this;
        if (!source.IsEmpty)
        {
            if (!separator.IsEmpty)
            {
                var (index, stride) = U8Searching.LastIndexOf(source.UnsafeSpan, separator.UnsafeSpan, comparer);

                if (index >= 0)
                {
                    return new(source, index, stride);
                }
            }

            return U8SplitPair.NotFound(source);
        }

        return default;
    }

    public U8SplitPair SplitLast<T>(ReadOnlySpan<byte> separator, T comparer)
        where T : IU8LastIndexOfOperator
    {
        var source = this;
        if (!source.IsEmpty)
        {
            if (separator.Length > 0)
            {
                var (index, stride) = U8Searching.LastIndexOf(source.UnsafeSpan, separator, comparer);

                if (index >= 0)
                {
                    var end = index + stride;
                    if (U8Info.IsContinuationByte(source.UnsafeRefAdd(index)) || (end < source.Length &&
                        U8Info.IsContinuationByte(source.UnsafeRefAdd(end))))
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

    internal U8SplitPair SplitLastUnchecked<T>(ReadOnlySpan<byte> separator, T comparer)
        where T : IU8LastIndexOfOperator
    {
        Debug.Assert(separator.Length > 0);

        var source = this;
        if (!source.IsEmpty)
        {
            var (index, stride) = U8Searching.LastIndexOf(source.UnsafeSpan, separator, comparer);

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
        ThrowHelpers.CheckAscii(separator);

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
        ValidatePossibleConstant(separator);

        return new(this, separator);
    }

    public ConfiguredU8Split<byte> Split(byte separator, U8SplitOptions options)
    {
        ThrowHelpers.CheckAscii(separator);

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
        ThrowHelpers.CheckAscii(separator);

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
        ValidatePossibleConstant(separator);

        return new(this, separator, comparer);
    }

    public ConfiguredU8Split<byte, T> Split<T>(byte separator, T comparer, U8SplitOptions options)
        where T : IU8ContainsOperator, IU8CountOperator, IU8IndexOfOperator
    {
        ThrowHelpers.CheckAscii(separator);

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
        ValidatePossibleConstant(separator);

        return new(this, separator, comparer, options);
    }

    public U8RefAnySplit SplitAny(ReadOnlySpan<byte> separators)
    {
        if (!Ascii.IsValid(separators))
        {
            ThrowHelpers.ArgumentException();
        }

        return new(this, separators);
    }
}
