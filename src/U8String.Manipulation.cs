using System.Buffers;
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
}
