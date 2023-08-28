using System.Runtime.InteropServices;
using System.Text;

using U8Primitives.Abstractions;
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
                return U8Manipulation.ConcatUnchecked(
                    left.UnsafeSpan,
                    right.UnsafeSpan);
            }

            return left;
        }

        return default;
    }

    public static U8String Concat(U8String left, ReadOnlySpan<byte> right)
    {
        if (!right.IsEmpty)
        {
            Validate(right);
            if (!left.IsEmpty)
            {
                return U8Manipulation.ConcatUnchecked(left.UnsafeSpan, right);
            }

            return new U8String(right, skipValidation: true);
        }

        return default;
    }

    public static U8String Concat(ReadOnlySpan<byte> left, U8String right)
    {
        if (!left.IsEmpty)
        {
            Validate(left);
            if (!right.IsEmpty)
            {
                return U8Manipulation.ConcatUnchecked(left, right.UnsafeSpan);
            }

            return new U8String(left, skipValidation: true);
        }

        return default;
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

    /// <inheritdoc />
    public void CopyTo(byte[] destination, int index)
    {
        var src = this;
        var dst = destination.AsSpan()[index..];
        if (src.Length > dst.Length)
        {
            ThrowHelpers.ArgumentOutOfRange(nameof(index));
        }

        src.UnsafeSpan.CopyTo(dst);
    }

    /// <summary>
    /// Normalizes current <see cref="U8String"/> to the specified Unicode normalization form (default: <see cref="NormalizationForm.FormC"/>).
    /// </summary>
    /// <returns>A new <see cref="U8String"/> normalized to the specified form.</returns>
    public U8String Normalize(NormalizationForm form = NormalizationForm.FormC)
    {
        throw new NotImplementedException();
    }

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

    public U8String ReplaceLineEndings()
    {
        var source = this;
        if (!source.IsEmpty)
        {
            if (!OperatingSystem.IsWindows())
            {
                return U8Manipulation.ReplaceCore(
                    source, "\r\n"u8, "\n"u8, validate: false);
            }

            // This needs manual loop which is sad
            throw new NotImplementedException();
        }

        return source;
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
            if ((start > 0 && U8Info.IsContinuationByte(source.UnsafeRefAdd(start))) || (
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
    /// Removes all leading and trailing whitespace characters from the current string.
    /// </summary>
    /// <returns>
    /// A sub-slice that remains after all whitespace characters
    /// are removed from the start and end of the current string.
    /// </returns>
    public U8String Trim()
    {
        // TODO: Optimize fast path on no whitespace
        // TODO 2: Do not convert to runes and have proper
        // whitespace LUT to evaluate code points in a branchless way
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

            var end = source.Length - 1;
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

            return U8Marshal.Slice(source, start, end - start + 1);
        }

        return default;
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
            var b = ptr;

            if (U8Info.IsAsciiByte(b) && !U8Info.IsAsciiWhitespace(b))
            {
                return source;
            }

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
            var source = deref.UnsafeSpan;

            var (replaceStart, resultLength) = converter.LowercaseHint(source);

            if ((uint)replaceStart < (uint)source.Length)
            {
                var lowercase = new byte[resultLength];
                var destination = lowercase.AsSpan();

                source[..replaceStart].CopyTo(destination);
                source = source.Slice(replaceStart);
                destination = destination.Slice(replaceStart);

                var convertedLength = converter.ToLower(source, destination);

                return new U8String(lowercase, 0, replaceStart + convertedLength);
            }
        }

        return deref;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String ToUpper<T>(T converter)
        where T : IU8CaseConverter
    {
        var deref = this;
        if (!deref.IsEmpty)
        {
            var source = deref.UnsafeSpan;
            var (replaceStart, resultLength) = converter.UppercaseHint(source);

            if ((uint)replaceStart < (uint)source.Length)
            {
                var uppercase = new byte[resultLength];
                var destination = uppercase.AsSpan();

                source[..replaceStart].CopyTo(destination);
                source = source.Slice(replaceStart);
                destination = destination.Slice(replaceStart);

                var convertedLength = converter.ToUpper(source, destination);

                return new U8String(uppercase, 0, replaceStart + convertedLength);
            }
        }

        return deref;
    }

    // TODO: docs
    // TODO 2: scan for lower/uppercase chars and only allocate if there are any
    public U8String ToLowerAscii()
    {
        var source = this;
        if (source.Length > 0)
        {
            var destination = new byte[source.Length];

            U8Manipulation.ToLowerAscii(
                ref source.UnsafeRef,
                ref MemoryMarshal.GetArrayDataReference(destination),
                (uint)source.Length);

            return new(destination, 0, source.Length);
        }

        return default;
    }

    public U8String ToUpperAscii()
    {
        var source = this;
        if (source.Length > 0)
        {
            var destination = new byte[source.Length];

            U8Manipulation.ToUpperAscii(
                ref source.UnsafeRef,
                ref MemoryMarshal.GetArrayDataReference(destination),
                (uint)source.Length);

            return new(destination, 0, source.Length);
        }

        return default;
    }
}