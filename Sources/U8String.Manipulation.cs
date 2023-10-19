using System.Runtime.InteropServices;
using System.Text;

using U8Primitives.Abstractions;
using U8Primitives.InteropServices;

namespace U8Primitives;

#pragma warning disable IDE0046, IDE0057 // Why: range slicing and ternary expressions do not produce desired codegen
public readonly partial struct U8String
{
    public static U8String Concat(U8String left, byte right)
    {
        if (!U8Info.IsAsciiByte(right))
        {
            ThrowHelpers.ArgumentOutOfRange(nameof(right));
        }

        return U8Manipulation.ConcatUnchecked(left, right);
    }

    public static U8String Concat(U8String left, char right)
    {
        ThrowHelpers.CheckSurrogate(right);

        return char.IsAscii(right)
            ? U8Manipulation.ConcatUnchecked(left, (byte)right)
            : U8Manipulation.ConcatUnchecked(left, new U8Scalar(right, checkAscii: false).AsSpan());
    }

    public static U8String Concat(U8String left, Rune right)
    {
        return right.IsAscii
            ? U8Manipulation.ConcatUnchecked(left, (byte)right.Value)
            : U8Manipulation.ConcatUnchecked(left, new U8Scalar(right, checkAscii: false).AsSpan());
    }

    public static U8String Concat(byte left, U8String right)
    {
        if (!U8Info.IsAsciiByte(left))
        {
            ThrowHelpers.ArgumentOutOfRange(nameof(left));
        }

        return U8Manipulation.ConcatUnchecked(left, right);
    }

    public static U8String Concat(char left, U8String right)
    {
        ThrowHelpers.CheckSurrogate(left);

        return char.IsAscii(left)
            ? U8Manipulation.ConcatUnchecked((byte)left, right)
            : U8Manipulation.ConcatUnchecked(new U8Scalar(left, checkAscii: false).AsSpan(), right);
    }

    public static U8String Concat(Rune left, U8String right)
    {
        return left.IsAscii
            ? U8Manipulation.ConcatUnchecked((byte)left.Value, right)
            : U8Manipulation.ConcatUnchecked(new U8Scalar(left, checkAscii: false).AsSpan(), right);
    }

    // TODO: Optimize/deduplicate Concat variants
    // TODO: Investigate if it is possible fold validation for u8 literals
    public static U8String Concat(U8String left, U8String right)
    {
        if (!left.IsEmpty)
        {
            if (!right.IsEmpty)
            {
                return U8Manipulation.ConcatUnchecked(
                    left.UnsafeSpan,
                    right.UnsafeSpan);
            }

            return left;
        }

        return right;
    }

    public static U8String Concat(U8String left, ReadOnlySpan<byte> right)
    {
        if (right.Length > 0)
        {
            Validate(right);
            if (!left.IsEmpty)
            {
                return U8Manipulation.ConcatUnchecked(left.UnsafeSpan, right);
            }

            return new U8String(right, skipValidation: true);
        }

        return left;
    }

    public static U8String Concat(ReadOnlySpan<byte> left, U8String right)
    {
        if (left.Length > 0)
        {
            Validate(left);
            if (!right.IsEmpty)
            {
                return U8Manipulation.ConcatUnchecked(left, right.UnsafeSpan);
            }

            return new U8String(left, skipValidation: true);
        }

        return right;
    }

    public static U8String Concat(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        var length = left.Length + right.Length;
        if (length > 0)
        {
            var value = new byte[length + 1];

            ref var dst = ref value.AsRef();
            left.CopyToUnsafe(ref dst);
            right.CopyToUnsafe(ref dst.Add(left.Length));

            Validate(value);
            return new U8String(value, 0, length);
        }

        return default;
    }

    public static U8String Concat(U8String[]? values)
    {
        return Concat(values.AsSpan());
    }

    public static U8String Concat(ReadOnlySpan<U8String> values)
    {
        if (values.Length > 1)
        {
            var length = 0;
            foreach (var value in values)
            {
                length += value.Length;
            }

            if (length > 0)
            {
                var value = new byte[length + 1];

                var offset = 0;
                ref var dst = ref value.AsRef();
                foreach (var source in values)
                {
                    source.AsSpan().CopyToUnsafe(ref dst.Add(offset));
                    offset += source.Length;
                }

                return new U8String(value, 0, length);
            }
        }
        else if (values.Length is 1)
        {
            return values[0];
        }

        return default;
    }

    public static U8String Concat(IEnumerable<U8String> values)
    {
        if (values is U8String[] array)
        {
            return Concat(array.AsSpan());
        }
        else if (values is List<U8String> list)
        {
            return Concat(CollectionsMarshal.AsSpan(list));
        }
        else if (values.TryGetNonEnumeratedCount(out var count))
        {
            if (count is 1)
            {
                return values.First();
            }
            else if (count is 0)
            {
                return default;
            }
        }

        return ConcatEnumerable(values);

        static U8String ConcatEnumerable(IEnumerable<U8String> values)
        {
            var builder = new ArrayBuilder();
            foreach (var value in values)
            {
                if (!value.IsEmpty)
                {
                    builder.Write(value.UnsafeSpan);
                }
            }

            var result = new U8String(builder.Written, skipValidation: true);

            builder.Dispose();
            return result;
        }
    }

    public static U8String Concat<T>(
        T[]? values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        return Concat<T>(values.AsSpan(), format, provider);
    }

    public static U8String Concat<T>(
        ReadOnlySpan<T> values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        if (values.Length > 1)
        {
            var builder = new ArrayBuilder();
            foreach (var value in values)
            {
                builder.Write(value, format, provider);
            }

            var result = new U8String(builder.Written, skipValidation: true);

            builder.Dispose();
            return result;
        }
        else if (values.Length is 1)
        {
            return Create(values[0], format, provider);
        }

        return default;
    }

    public static U8String Concat<T>(
        IEnumerable<T> values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        if (values is T[] array)
        {
            return Concat<T>(array.AsSpan(), format, provider);
        }
        else if (values is List<T> list)
        {
            return Concat<T>(CollectionsMarshal.AsSpan(list), format, provider);
        }
        else if (values.TryGetNonEnumeratedCount(out var count))
        {
            if (count is 1)
            {
                return Create(values.First(), format, provider);
            }
            else if (count is 0)
            {
                return default;
            }
        }

        return ConcatEnumerable(values, format, provider);

        static U8String ConcatEnumerable(
            IEnumerable<T> values,
            ReadOnlySpan<char> format,
            IFormatProvider? provider)
        {
            var builder = new ArrayBuilder();
            foreach (var value in values)
            {
                builder.Write(value, format, provider);
            }

            var result = new U8String(builder.Written, skipValidation: true);

            builder.Dispose();
            return result;
        }
    }

    /// <inheritdoc />
    public void CopyTo(byte[] destination, int index)
    {
        AsSpan().CopyTo(destination.AsSpan()[index..]);
    }

    public void CopyTo(Span<byte> destination)
    {
        AsSpan().CopyTo(destination);
    }

    public static U8String Join(byte separator, U8String[]? values) => Join(separator, values.AsSpan());

    public static U8String Join(byte separator, ReadOnlySpan<U8String> values)
    {
        if (!U8Info.IsAsciiByte(separator))
        {
            ThrowHelpers.ArgumentOutOfRange(nameof(separator));
        }

        return U8Manipulation.Join(separator, values);
    }

    public static U8String Join(byte separator, IEnumerable<U8String> values)
    {
        if (!U8Info.IsAsciiByte(separator))
        {
            ThrowHelpers.ArgumentOutOfRange(nameof(separator));
        }

        return U8Manipulation.Join(separator, values);
    }

    public static U8String Join(char separator, U8String[]? values) => Join(separator, values.AsSpan());

    public static U8String Join(char separator, ReadOnlySpan<U8String> values)
    {
        ThrowHelpers.CheckSurrogate(separator);

        return char.IsAscii(separator)
            ? U8Manipulation.Join((byte)separator, values)
            : U8Manipulation.Join(new U8Scalar(separator, checkAscii: false).AsSpan(), values);
    }

    public static U8String Join(char separator, IEnumerable<U8String> values)
    {
        ThrowHelpers.CheckSurrogate(separator);

        return char.IsAscii(separator)
            ? U8Manipulation.Join((byte)separator, values)
            : U8Manipulation.Join(new U8Scalar(separator, checkAscii: false).AsSpan(), values);
    }

    public static U8String Join(Rune separator, U8String[]? values) => Join(separator, values.AsSpan());

    public static U8String Join(Rune separator, ReadOnlySpan<U8String> values)
    {
        return separator.IsAscii
            ? U8Manipulation.Join((byte)separator.Value, values)
            : U8Manipulation.Join(new U8Scalar(separator, checkAscii: false).AsSpan(), values);
    }

    public static U8String Join(Rune separator, IEnumerable<U8String> values)
    {
        return separator.IsAscii
            ? U8Manipulation.Join((byte)separator.Value, values)
            : U8Manipulation.Join(new U8Scalar(separator, checkAscii: false).AsSpan(), values);
    }

    public static U8String Join(ReadOnlySpan<byte> separator, U8String[]? values) => Join(separator, values.AsSpan());

    public static U8String Join(ReadOnlySpan<byte> separator, ReadOnlySpan<U8String> values)
    {
        Validate(separator);
        return U8Manipulation.Join(separator, values);
    }

    public static U8String Join(ReadOnlySpan<byte> separator, IEnumerable<U8String> values)
    {
        Validate(separator);
        return U8Manipulation.Join(separator, values);
    }

    public static U8String Join<T>(
        byte separator,
        T[]? values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        return Join<T>(separator, values.AsSpan(), format, provider);
    }

    public static U8String Join<T>(
        byte separator,
        ReadOnlySpan<T> values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        if (!U8Info.IsAsciiByte(separator))
        {
            ThrowHelpers.ArgumentOutOfRange(nameof(separator));
        }

        return U8Manipulation.Join(separator, values, format, provider);
    }

    public static U8String Join<T>(
        byte separator,
        IEnumerable<T> values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        if (!U8Info.IsAsciiByte(separator))
        {
            ThrowHelpers.ArgumentOutOfRange(nameof(separator));
        }

        return U8Manipulation.Join(separator, values, format, provider);
    }

    public static U8String Join<T>(
        char separator,
        T[]? values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        return Join<T>(separator, values.AsSpan(), format, provider);
    }

    public static U8String Join<T>(
        char separator,
        ReadOnlySpan<T> values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        ThrowHelpers.CheckSurrogate(separator);

        return char.IsAscii(separator)
            ? U8Manipulation.Join((byte)separator, values, format, provider)
            : U8Manipulation.Join(new U8Scalar(separator, checkAscii: false).AsSpan(), values, format, provider);
    }

    public static U8String Join<T>(
        char separator,
        IEnumerable<T> values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        ThrowHelpers.CheckSurrogate(separator);

        return char.IsAscii(separator)
            ? U8Manipulation.Join((byte)separator, values, format, provider)
            : U8Manipulation.Join(new U8Scalar(separator, checkAscii: false).AsSpan(), values, format, provider);
    }

    public static U8String Join<T>(
        Rune separator,
        T[]? values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        return Join<T>(separator, values.AsSpan(), format, provider);
    }

    public static U8String Join<T>(
        Rune separator,
        ReadOnlySpan<T> values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        return separator.IsAscii
            ? U8Manipulation.Join((byte)separator.Value, values, format, provider)
            : U8Manipulation.Join(new U8Scalar(separator, checkAscii: false).AsSpan(), values, format, provider);
    }

    public static U8String Join<T>(
        Rune separator,
        IEnumerable<T> values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        return separator.IsAscii
            ? U8Manipulation.Join((byte)separator.Value, values, format, provider)
            : U8Manipulation.Join(new U8Scalar(separator, checkAscii: false).AsSpan(), values, format, provider);
    }

    public static U8String Join<T>(
        ReadOnlySpan<byte> separator,
        T[]? values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        return Join<T>(separator, values.AsSpan(), format, provider);
    }

    public static U8String Join<T>(
        ReadOnlySpan<byte> separator,
        ReadOnlySpan<T> values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        Validate(separator);
        return U8Manipulation.Join(separator, values, format, provider);
    }

    public static U8String Join<T>(
        ReadOnlySpan<byte> separator,
        IEnumerable<T> values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        Validate(separator);
        return U8Manipulation.Join(separator, values, format, provider);
    }

    /// <summary>
    /// Normalizes current <see cref="U8String"/> to the specified Unicode normalization form (default: <see cref="NormalizationForm.FormC"/>).
    /// </summary>
    /// <returns>A new <see cref="U8String"/> normalized to the specified form.</returns>
    internal U8String Normalize(NormalizationForm form = NormalizationForm.FormC)
    {
        throw new NotImplementedException();
    }

    public U8String NullTerminate()
    {
        var deref = this;
        if (!deref.IsEmpty)
        {
            var (value, offset, length) = deref;
            ref var end = ref deref.UnsafeRefAdd(length - 1);

            U8String result;
            if (end is 0)
            {
                result = deref;
            }
            else if ((uint)(offset + length) < (uint)value!.Length &&
                end.Add(1) is 0)
            {
                result = new(deref._value, offset, deref.Length + 1);
            }
            else
            {
                var bytes = new byte[length + 1];
                value.SliceUnsafe(offset, length).CopyToUnsafe(ref bytes.AsRef());
                result = new(bytes, 0, length + 1);
            }

            return result;
        }

        return U8Constants.NullByte;
    }

    /// <inheritdoc cref="Remove(U8String)"/>
    public U8String Remove(byte value) => U8Manipulation.Remove(this, value);

    /// <inheritdoc cref="Remove(U8String)"/>
    public U8String Remove(char value)
    {
        ThrowHelpers.CheckSurrogate(value);

        return char.IsAscii(value)
            ? U8Manipulation.Remove(this, (byte)value)
            : U8Manipulation.Remove(this, new U8Scalar(value, checkAscii: false).AsSpan(), validate: false);
    }

    /// <inheritdoc cref="Remove(U8String)"/>
    public U8String Remove(Rune value) => value.IsAscii
        ? U8Manipulation.Remove(this, (byte)value.Value)
        : U8Manipulation.Remove(this, new U8Scalar(value, checkAscii: false).AsSpan());

    /// <inheritdoc cref="Remove(U8String)"/>
    public U8String Remove(ReadOnlySpan<byte> value) => value.Length is 1
        ? U8Manipulation.Remove(this, value[0])
        : U8Manipulation.Remove(this, value);

    /// <summary>
    /// Removes all occurrences of <paramref name="value"/> from the current <see cref="U8String"/>.
    /// </summary>
    /// <param name="value">The element to remove from the current <see cref="U8String"/>.</param>
    public U8String Remove(U8String value) => value.Length is 1
        ? U8Manipulation.Remove(this, value.UnsafeRef)
        : U8Manipulation.Remove(this, value, validate: false);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String Replace(byte oldValue, byte newValue)
    {
        return U8Manipulation.Replace(this, oldValue, newValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String Replace(char oldValue, char newValue)
    {
        return U8Manipulation.Replace(this, oldValue, newValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String Replace(Rune oldValue, Rune newValue)
    {
        return U8Manipulation.Replace(this, oldValue, newValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String Replace(ReadOnlySpan<byte> oldValue, ReadOnlySpan<byte> newValue)
    {
        return U8Manipulation.Replace(this, oldValue, newValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String Replace(U8String oldValue, U8String newValue)
    {
        return U8Manipulation.Replace(this, oldValue, newValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String ReplaceLineEndings()
    {
        return OperatingSystem.IsWindows()
            ? U8Manipulation.LineEndingsToCRLF(this)
            : U8Manipulation.LineEndingsToLF(this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String ReplaceLineEndings(ReadOnlySpan<byte> lineEnding)
    {
        if (!BitConverter.IsLittleEndian)
        {
            ThrowHelpers.NotSupportedBigEndian();
        }

        if (lineEnding.IsEmpty)
        {
            return U8Manipulation.StripLineEndings(this);
        }
        else if (lineEnding is [(byte)'\n'])
        {
            return U8Manipulation.LineEndingsToLF(this);
        }
        //else if (lineEnding is [(byte)'\r', (byte)'\n'])
        else if (lineEnding.Length is 2
            && lineEnding.AsRef().Cast<byte, ushort>() is 0xA0D)
        {
            return U8Manipulation.LineEndingsToCRLF(this);
        }
        else
        {
            Validate(lineEnding);
            return U8Manipulation.LineEndingsToCustom(this, lineEnding);
        }
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
        if ((uint)start > (uint)source.Length)
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        var length = source.Length - start;
        if (length > 0)
        {
            if (U8Info.IsContinuationByte(in source.UnsafeRefAdd(start)))
            {
                ThrowHelpers.ArgumentOutOfRange();
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
        if ((uint)(start + length) > (uint)source.Length)
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        if (length > 0)
        {
            if (U8Info.IsContinuationByte(source.UnsafeRefAdd(start)) || (
                length < source.Length && U8Info.IsContinuationByte(source.UnsafeRefAdd(start + length))))
            {
                ThrowHelpers.ArgumentOutOfRange();
            }

            return new(source._value, source.Offset + start, length);
        }

        return default;
    }

    /// <summary>
    /// Removes all leading and trailing whitespace characters from the current string.
    /// </summary>
    /// <returns>
    /// A sub-slice that remains after all whitespace characters
    /// are removed from the start and end of the current string.
    /// </returns>
    public U8String Trim()
    {
        // TODO 2: Do not convert to runes and have proper
        // whitespace LUT to evaluate code points in a branchless way
        var source = this;
        if (!source.IsEmpty)
        {
            ref var ptr = ref source.UnsafeRef;
            var (start, end) = (0, source.Length - 1);
            var last = ptr.Add(end);

            if (U8Info.IsAsciiByte(in ptr) && !U8Info.IsAsciiWhitespace(in ptr))
            {
                if (U8Info.IsAsciiByte(last) && !U8Info.IsAsciiWhitespace(last))
                {
                    return source;
                }
                else --end;
            } // Can't increment start because TrimCore expects non-continuation byte

            return TrimCore(source._value, source.Offset + start, end);
        }

        return default;

        static U8String TrimCore(byte[] source, int start, int end)
        {
            ref var ptr = ref source.AsRef();
            while (start <= end)
            {
                if (!U8Info.IsWhitespaceRune(ref ptr.Add(start), out var size))
                {
                    break;
                }
                start += size;
            }

            for (var endSearch = end; endSearch >= start; endSearch--)
            {
                var b = ptr.Add(endSearch);
                if (!U8Info.IsContinuationByte(b))
                {
                    if (U8Info.IsAsciiByte(b)
                        ? U8Info.IsAsciiWhitespace(b)
                        : U8Info.IsNonAsciiWhitespace(ref ptr.Add(end), out _))
                    {
                        // Save the last found whitespace code point offset and continue searching
                        // for more whitspace byte sequences from their end. If we don't do this,
                        // we will end up trimming away continuation bytes at the end of the string.
                        end = endSearch - 1;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return new U8String(source, start, end - start + 1);
        }
    }

    /// <summary>
    /// Removes all leading whitespace characters from the current string.
    /// </summary>
    /// <returns>
    /// A sub-slice that remains after all whitespace characters
    /// are removed from the start of the current string.
    /// </returns>
    public U8String TrimStart()
    {
        var source = this;
        if (!source.IsEmpty)
        {
            ref var ptr = ref source.UnsafeRef;

            var start = 0;
            while (start < source.Length)
            {
                if (!U8Info.IsWhitespaceRune(ref ptr.Add(start), out var size))
                {
                    break;
                }
                start += size;
            }

            return U8Marshal.Slice(source, start);
        }

        return default;
    }

    /// <summary>
    /// Removes all trailing whitespace characters from the current string.
    /// </summary>
    /// <returns>
    /// A sub-slice that remains after all whitespace characters
    /// are removed from the end of the current string.
    /// </returns>
    public U8String TrimEnd()
    {
        var source = this;
        if (!source.IsEmpty)
        {
            ref var ptr = ref source.UnsafeRef;

            var end = source.Length - 1;
            for (var endSearch = end; endSearch >= 0; endSearch--)
            {
                var b = ptr.Add(endSearch);
                if (!U8Info.IsContinuationByte(b))
                {
                    if (U8Info.IsAsciiByte(b)
                        ? U8Info.IsAsciiWhitespace(b)
                        : U8Info.IsNonAsciiWhitespace(ref ptr.Add(end), out _))
                    {
                        end = endSearch - 1;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return U8Marshal.Slice(source, 0, end + 1);
        }

        return default;
    }

    /// <summary>
    /// Removes all leading and trailing ASCII whitespace characters from the current string.
    /// </summary>
    /// <returns>
    /// A sub-slice that remains after all ASCII whitespace characters
    /// are removed from the start and end of the current string.
    /// </returns>
    public U8String TrimAscii()
    {
        var source = this;
        var range = Ascii.Trim(source);

        return U8Marshal.Slice(source, range);
    }

    /// <summary>
    /// Removes all the leading ASCII whitespace characters from the current string.
    /// </summary>
    /// <returns>
    /// A sub-slice that remains after all whitespace characters
    /// are removed from the start of the current string.
    /// </returns>
    public U8String TrimStartAscii()
    {
        var source = this;
        var range = Ascii.TrimStart(source);

        return U8Marshal.Slice(source, range);
    }

    /// <summary>
    /// Removes all the trailing ASCII whitespace characters from the current string.
    /// </summary>
    /// <returns>
    /// A sub-slice that remains after all whitespace characters
    /// are removed from the end of the current string.
    /// </returns>
    public U8String TrimEndAscii()
    {
        var source = this;
        var range = Ascii.TrimEnd(source);

        return U8Marshal.Slice(source, range);
    }

    // TODO:
    // - Complete impl. depends on porting of InlineArray-based array builder for letters
    // which have different lengths in upper/lower case.
    // - Remove/rename to ToLowerFallback or move to something like "FallbackInvariantComparer"
    // clearly indicating it being slower and inferior alternative to proper implementations
    // which call into ICU/NLS/Hybrid-provided case change exports.
    public U8String ToLower<T>(T converter)
        where T : IU8CaseConverter
    {
        // 1. Estimate the start offset of the conversion (first char requiring case change)
        // 2. Estimate the length of the conversion (the length of the resulting segment after case change)
        // 3. Allocate the resulting buffer and copy the pre-offset segment
        // 4. Perform the conversion which writes to the remainder segment of the buffer
        // 5. Return the resulting buffer as a new string

        var deref = this;
        if (!deref.IsEmpty)
        {
            var trusted = U8CaseConversion.IsTrustedImplementation(converter);
            var source = deref.UnsafeSpan;

            var (replaceStart, resultLength) = converter.LowercaseHint(source);

            int convertedLength;
            if ((uint)replaceStart < (uint)source.Length)
            {
                var lowercase = new byte[resultLength + 1];
                var destination = lowercase.AsSpan();

                if (trusted)
                {
                    source
                        .SliceUnsafe(0, replaceStart)
                        .CopyTo(destination.SliceUnsafe(0, source.Length));
                    source = source.SliceUnsafe(replaceStart);
                    destination = destination.SliceUnsafe(replaceStart, source.Length);

                    convertedLength = converter.ToLower(source, destination) + replaceStart;
                }
                else
                {
                    source[..replaceStart]
                        .CopyTo(destination.SliceUnsafe(0, source.Length));
                    source = source.Slice(replaceStart);
                    destination = destination.Slice(replaceStart, source.Length);

                    convertedLength = converter.ToLower(source, destination) + replaceStart;

                    if (convertedLength > resultLength)
                    {
                        // TODO: EH UX
                        ThrowHelpers.ArgumentOutOfRange();
                    }
                }

                return new U8String(lowercase, 0, convertedLength);
            }
        }

        return deref;
    }

    public U8String ToUpper<T>(T converter)
        where T : IU8CaseConverter
    {
        var deref = this;
        if (!deref.IsEmpty)
        {
            var trusted = U8CaseConversion.IsTrustedImplementation(converter);
            var source = deref.UnsafeSpan;

            var (replaceStart, resultLength) = converter.UppercaseHint(source);

            int convertedLength;
            if ((uint)replaceStart < (uint)source.Length)
            {
                var uppercase = new byte[resultLength + 1];
                var destination = uppercase.AsSpan();

                if (trusted)
                {
                    source
                        .SliceUnsafe(0, replaceStart)
                        .CopyTo(destination.SliceUnsafe(0, source.Length));
                    source = source.SliceUnsafe(replaceStart);
                    destination = destination.SliceUnsafe(replaceStart, source.Length);

                    convertedLength = converter.ToUpper(source, destination) + replaceStart;
                }
                else
                {
                    source[..replaceStart]
                        .CopyTo(destination.SliceUnsafe(0, source.Length));
                    source = source.Slice(replaceStart);
                    destination = destination.Slice(replaceStart, source.Length);

                    convertedLength = converter.ToUpper(source, destination) + replaceStart;

                    if (convertedLength > resultLength)
                    {
                        // TODO: EH UX
                        ThrowHelpers.ArgumentOutOfRange();
                    }
                }

                return new U8String(uppercase, 0, convertedLength);
            }
        }

        return deref;
    }

    // TODO: docs
    public U8String ToLowerAscii()
    {
        return ToLower(U8CaseConversion.Ascii);
    }

    public U8String ToUpperAscii()
    {
        return ToUpper(U8CaseConversion.Ascii);
    }
}
