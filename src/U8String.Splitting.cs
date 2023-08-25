using System.Text;

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
        : SplitFirstUnchecked(U8Scalar.Create(separator, checkAscii: false).AsSpan());

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
        : SplitLastUnchecked(U8Scalar.Create(separator, checkAscii: false).AsSpan());

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

    public U8Split<char> Split(char separator)
    {
        if (char.IsSurrogate(separator))
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

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
        if (char.IsSurrogate(separator))
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        return new(this, separator, options);
    }

    public ConfiguredU8Split<Rune> Split(Rune separator, U8SplitOptions options) => new(this, separator, options);

    public ConfiguredU8Split Split(U8String separator, U8SplitOptions options)
    {
        return new(this, separator, options);
    }
}
