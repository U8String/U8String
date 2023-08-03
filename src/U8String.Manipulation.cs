using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

using U8Primitives.InteropServices;

namespace U8Primitives;

#pragma warning disable IDE0046, IDE0057 // Why: range slicing and ternary expressions do not produce desired codegen
public readonly partial struct U8String
{
    // TODO: Optimize/deduplicate Concat variants
    // TODO: Investigate if it is possible fold validation for u8 literals
    public static U8String Concat(U8String left, U8String right)
    {
        if (!left.IsEmpty)
        {
            if (!right.IsEmpty)
            {
                var length = left.Length + right.Length;
                var value = new byte[length];

                left.UnsafeSpan.CopyTo(value);
                right.UnsafeSpan.CopyTo(value.AsSpan(left.Length));

                return new U8String(value, 0, length);
            }

            return left;
        }

        return right;
    }

    public static U8String Concat(U8String left, ReadOnlySpan<byte> right)
    {
        if (!right.IsEmpty)
        {
            Validate(right);
            if (!left.IsEmpty)
            {
                var length = left.Length + right.Length;
                var value = new byte[length];

                left.UnsafeSpan.CopyTo(value);
                right.CopyTo(value.AsSpan(left.Length));

                return new U8String(value, 0, length);
            }

            return new U8String(right, skipValidation: true);
        }

        return left;
    }

    public static U8String Concat(ReadOnlySpan<byte> left, U8String right)
    {
        if (!left.IsEmpty)
        {
            Validate(left);
            if (!right.IsEmpty)
            {
                var length = left.Length + right.Length;
                var value = new byte[length];

                left.CopyTo(value);
                right.UnsafeSpan.CopyTo(value.AsSpan(left.Length));

                return new U8String(value, 0, length);
            }

            return new U8String(left, skipValidation: true);
        }

        return right;
    }

    public static U8String Concat(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        var length = left.Length + right.Length;
        if (length != 0)
        {
            var value = new byte[length];

            left.CopyTo(value);
            right.CopyTo(value.SliceUnsafe(left.Length, right.Length));

            Validate(value);
            return new U8String(value, 0, length);
        }

        return default;
    }

    /// <summary>
    /// Normalizes current <see cref="U8String"/> to the specified Unicode normalization form (default: <see cref="NormalizationForm.FormC"/>).
    /// </summary>
    /// <returns>A new <see cref="U8String"/> normalized to the specified form.</returns>
    public U8String Normalize(NormalizationForm form = NormalizationForm.FormC)
    {
        throw new NotImplementedException();
    }

    public U8String Replace(byte oldValue, byte newValue)
    {
        var source = this;
        if (!source.IsEmpty)
        {
            var current = source.UnsafeSpan;
            var firstReplace = current.IndexOf(oldValue);
            if (firstReplace < 0)
            {
                return source;
            }

            var replaced = new byte[source.Length];
            var destination = replaced.AsSpan();

            current
                .SliceUnsafe(0, firstReplace)
                .CopyTo(destination);

            destination = destination.SliceUnsafe(firstReplace);
            current
                .SliceUnsafe(firstReplace)
                .Replace(destination, oldValue, newValue);

            // Old and new bytes which individually are invalid unicode scalar values
            // are allowed if the replacement produces a valid UTF-8 sequence.
            Validate(replaced);
            return new(replaced, 0, source.Length);
        }

        return default;
    }

    /// <inheritdoc />
    public void CopyTo(byte[] destination, int index)
    {
        var source = this;
        if ((uint)index > (uint)destination.Length)
        {
            ThrowHelpers.ArgumentOutOfRange(nameof(index));
        }

        if (destination.Length - index < source.Length)
        {
            // TODO: EH UX
            // ThrowHelpers.Argument(nameof(destination), "Destination buffer is too small.");
        }

        source.UnsafeSpan.CopyTo(destination.AsSpan(index));
    }

    // Selectively inlining some overloads which are expected
    // to take byte or utf-8 constant literals.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SplitPair SplitFirst(char separator) => char.IsAscii(separator)
        ? SplitFirst((byte)separator)
        : SplitFirst(new Rune(separator));

    public SplitPair SplitFirst(Rune separator)
    {
        var source = this;
        if (!source.IsEmpty)
        {
            var separatorBytes = separator.ToUtf8(out _);

            var span = source.UnsafeSpan;
            var index = span.IndexOf(separatorBytes);
            if (index >= 0)
            {
                return new(source, index, separatorBytes.Length);
            }

            return SplitPair.NotFound(source);
        }

        return default;
    }

    // TODO: Reconsider the behavior on empty separator - what do Rust and Go do?
    // Should an empty separator effectively match no bytes which would be at the
    // start of the string, putting source in the remainder? (same with SplitLast and ROS overloads)
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
    public SplitPair SplitLast(char separator) => char.IsAscii(separator)
        ? SplitLast((byte)separator)
        : SplitLast(new Rune(separator));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    public SplitPair SplitLast(Rune separator)
    {
        var source = this;
        if (!source.IsEmpty)
        {
            var separatorBytes = separator.ToUtf8(out _);

            var span = source.UnsafeSpan;
            var index = span.LastIndexOf(separatorBytes);
            if (index >= 0)
            {
                return new(source, index, separatorBytes.Length);
            }

            return SplitPair.NotFound(source);
        }

        return default;
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    public SplitCollection<byte> Split(byte separator, U8SplitOptions options = U8SplitOptions.None)
    {
        var split = default(SplitCollection<byte>);
        var source = this;
        if (!source.IsEmpty)
        {
            split = new(source, separator, options);
        }

        return split;
    }

    public SplitCollection<char> Split(char separator, U8SplitOptions options = U8SplitOptions.None)
    {
        var split = default(SplitCollection<char>);
        var source = this;
        if (!source.IsEmpty)
        {
            split = new(source, separator, options);
        }

        return split;
    }

    public SplitCollection<Rune> Split(Rune separator, U8SplitOptions options = U8SplitOptions.None)
    {
        var split = default(SplitCollection<Rune>);
        var source = this;
        if (!source.IsEmpty)
        {
            split = new(source, separator, options);
        }

        return split;
    }

    public SplitCollection<U8String> Split(U8String separator, U8SplitOptions options = U8SplitOptions.None)
    {
        var split = default(SplitCollection<U8String>);
        var source = this;
        if (!source.IsEmpty)
        {
            split = new(source, separator, options);
        }

        return split;
    }

    public SplitCollection<byte[]> Split(ReadOnlySpan<byte> separator, U8SplitOptions options = U8SplitOptions.None)
    {
        if (!IsValid(separator))
        {
            // TODO: EH UX
            ThrowHelpers.InvalidSplit();
        }

        var split = default(SplitCollection<byte[]>);
        var source = this;
        if (!source.IsEmpty)
        {
            split = new(source, separator.ToArray(), options);
        }

        return split;
    }

    /// <summary>
    /// Retrieves a substring from this instance. The substring starts at a specified
    /// character position and continues to the end of the string.
    /// </summary>
    /// <param name="start">The zero-based starting character position of a substring in this instance.</param>
    /// <returns>A substring view that begins at <paramref name="start"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="start"/> is less than zero or greater than the length of this instance.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The resulting substring splits at a UTF-8 code point boundary and would result in an invalid UTF-8 string.
    /// </exception>
    public U8String Slice(int start)
    {
        var source = this;
        // From ReadOnly/Span<T> Slice(int) implementation
        if ((ulong)(uint)start > (ulong)(uint)source.Length)
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        var length = source.Length - start;
        if (length > 0)
        {
            if (U8Info.IsContinuationByte(in source.UnsafeRefAdd(start)))
            {
                ThrowHelpers.InvalidSplit();
            }

            return new(source._value, source.Offset + start, length);
        }

        return default;
    }

    /// <summary>
    /// Retrieves a substring from this instance. The substring starts at a specified
    /// character position and has a specified length.
    /// </summary>
    /// <param name="start">The zero-based starting character position of a substring in this instance.</param>
    /// <param name="length">The number of bytes in the substring.</param>
    /// <returns>A substring view that begins at <paramref name="start"/> and has <paramref name="length"/> bytes.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="start"/> or <paramref name="length"/> is less than zero, or the sum of <paramref name="start"/> and <paramref name="length"/> is greater than the length of the current instance.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The resulting substring splits at a UTF-8 code point boundary and would result in an invalid UTF-8 string.
    /// </exception>
    public U8String Slice(int start, int length)
    {
        var source = this;
        // From ReadOnly/Span<T> Slice(int, int) implementation
        if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)source.Length)
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        var result = default(U8String);
        if (length > 0)
        {
            // TODO: Is there really no way to get rid of length < source.Length when checking the last+1 byte?
            if (U8Info.IsContinuationByte(source.UnsafeRefAdd(start)) || (
                length < source.Length && U8Info.IsContinuationByte(source.UnsafeRefAdd(start + length))))
            {
                // TODO: Exception message UX
                ThrowHelpers.InvalidSplit();
            }

            result = new(source._value, source.Offset + start, length);
        }

        return result;
    }

    /// <summary>
    /// Removes all leading and trailing ASCII white-space characters from the current string.
    /// </summary>
    /// <returns>
    /// A substring that remains after all ASCII white-space characters
    /// are removed from the start and end of the current string.
    /// </returns>
    public U8String TrimAscii()
    {
        var source = this;
        var range = Ascii.Trim(source);

        return !range.IsEmpty()
            ? U8Marshal.Slice(source, range)
            : default;
    }

    /// <summary>
    /// Removes all the leading ASCII white-space characters from the current string.
    /// </summary>
    /// <returns>
    /// A substring that remains after all white-space characters
    /// are removed from the start of the current string.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String TrimStartAscii()
    {
        var source = this;
        var range = Ascii.TrimStart(source);

        return !range.IsEmpty()
            ? U8Marshal.Slice(source, range)
            : default;
    }

    /// <summary>
    /// Removes all the trailing ASCII white-space characters from the current string.
    /// </summary>
    /// <returns>
    /// A substring that remains after all white-space characters
    /// are removed from the end of the current string.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String TrimEndAscii()
    {
        var source = this;
        var range = Ascii.TrimEnd(source);

        return !range.IsEmpty()
            ? U8Marshal.Slice(source, range)
            : default;
    }

    /// <summary>
    /// Returns a copy of this ASCII string converted to lower case.
    /// </summary>
    /// <returns>A lowercase equivalent of the current ASCII string.</returns>
    /// <exception cref="ArgumentException">
    /// The current string is not a valid ASCII sequence.
    /// </exception>
    public U8String ToLowerAscii()
    {
        var source = this;
        if (!source.IsEmpty)
        {
            var span = source.UnsafeSpan;
            var destination = new byte[span.Length];
            var result = Ascii.ToLower(span, destination, out _);
            if (result is OperationStatus.InvalidData)
            {
                ThrowHelpers.InvalidAscii();
            }

            return new U8String(destination, 0, span.Length);
        }

        return default;
    }

    /// <summary>
    /// Returns a copy of this ASCII string converted to upper case.
    /// </summary>
    /// <returns>The uppercase equivalent of the current ASCII string.</returns>
    /// <exception cref="ArgumentException">
    /// The current string is not a valid ASCII sequence.
    /// </exception>
    public U8String ToUpperAscii()
    {
        var source = this;
        if (!source.IsEmpty)
        {
            var span = source.UnsafeSpan;
            var destination = new byte[span.Length];
            var result = Ascii.ToUpper(span, destination, out _);
            if (result is OperationStatus.InvalidData)
            {
                ThrowHelpers.InvalidAscii();
            }

            return new U8String(destination, 0, span.Length);
        }

        return default;
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

    // TODO: Is struct okay here? It is expected get boxed only once or twice per split
    public struct SplitCollection<TSeparator> // : ICollection<U8String>
    {
        readonly U8String _value;
        readonly TSeparator? _separator; // Maybe just box the separator to allow a union-like behavior?
        readonly U8SplitOptions _options;
        int _count;

        // TODO: Move value.IsEmpty -> count = 0 check here
        internal SplitCollection(
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
                        return U8Shared.CountSegments(value, separator);
                    }

                    return U8Shared.CountSegments(value, separator, options);
                }
            }
        }

        public bool Contains(U8String item)
        {
            var separator = _separator;
            var isItemInvalid = U8Shared.Contains(item, separator);

            return !isItemInvalid && _value.Contains(item);
        }
    }
}
