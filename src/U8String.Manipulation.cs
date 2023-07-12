using System.Buffers;

namespace U8Primitives;

public readonly partial struct U8String
{
    // TODO: Optimize/deduplicate Concat variants
    // TODO: Investigate if it is possible fold validation for u8 literals
    public static U8String Concat(U8String left, U8String right)
    {
        if (left.IsEmpty)
        {
            return right;
        }

        if (right.IsEmpty)
        {
            return left;
        }

        var length = left.InnerLength + right.InnerLength;
        var value = new byte[length];

        left.AsSpan().CopyTo(value);
        right.AsSpan().CopyTo(value.AsSpan((int)left.InnerLength));

        return new U8String(value, 0, length);
    }

    public static U8String Concat(U8String left, ReadOnlySpan<byte> right)
    {
        if (right.IsEmpty)
        {
            return left;
        }

        Validate(right);
        if (left.IsEmpty)
        {
            return new U8String(right, skipValidation: true);
        }

        var length = (uint)(left.Length + right.Length);
        var value = new byte[length];

        left.AsSpan().CopyTo(value);
        right.CopyTo(value.AsSpan(left.Length));

        return new U8String(value, 0, length);
    }

    public static U8String Concat(ReadOnlySpan<byte> left, U8String right)
    {
        if (left.IsEmpty)
        {
            return right;
        }

        Validate(left);
        if (right.IsEmpty)
        {
            return new U8String(left, skipValidation: true);
        }

        var length = (uint)(left.Length + right.Length);
        var value = new byte[length];

        left.CopyTo(value);
        right.AsSpan().CopyTo(value.AsSpan(left.Length));

        return new U8String(value, 0, length);
    }

    public static U8String Concat(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        Validate(left);
        if (right.IsEmpty)
        {
            return new U8String(left, skipValidation: true);
        }

        Validate(right);
        if (left.IsEmpty)
        {
            return new U8String(right);
        }

        var length = (uint)(left.Length + right.Length);
        var value = new byte[length];

        left.CopyTo(value);
        right.CopyTo(value.AsSpan(left.Length));

        return new U8String(value, 0, length);
    }

    public U8String Replace(byte oldValue, byte newValue)
    {
        if (IsEmpty)
        {
            return this;
        }

        var current = AsSpan();
        var firstReplace = current.IndexOf(oldValue);
        if (firstReplace < 0)
        {
            return this;
        }

        var replaced = new byte[InnerLength].AsSpan();
        current[firstReplace..].Replace(
            replaced[firstReplace..],
            oldValue,
            newValue);

        // Pass to ctor which will perform validation.
        // Old and new bytes which individually are invalid unicode scalar values are allowed
        // if the replacement produces a valid UTF-8 sequence.
        return new U8String(replaced);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (U8String Segment, U8String Remainder) SplitFirst(byte separator)
    {
        if (!Rune.IsValid(separator))
        {
            // TODO: EH UX
            ThrowHelpers.ArgumentOutOfRange();
        }

        if (IsEmpty)
        {
            return default;
        }

        var span = AsSpan();
        var index = span.IndexOf(separator);
        return index >= 0
            ? (this[..index], this[(index + 1)..])
            : (this, default);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (U8String Segment, U8String Remainder) SplitFirst(Rune separator)
    {
        if (IsEmpty)
        {
            return default;
        }

        var separatorBytes = (stackalloc byte[4]);
        var separatorLength = separator.EncodeToUtf8(separatorBytes);

        var span = AsSpan();
        var index = span.IndexOf(separatorBytes[..separatorLength]);
        return index >= 0
            ? (this[..index], this[(index + separatorLength)..])
            : (this, default);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (U8String Segment, U8String Remainder) SplitFirst(U8String separator)
    {
        if (IsEmpty)
        {
            return default;
        }

        if (separator.IsEmpty)
        {
            return (this, default);
        }

        var span = AsSpan();
        var index = span.IndexOf(separator);
        return index >= 0
            ? (this[..index], this[(index + 1)..])
            : (this, default);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (U8String Segment, U8String Remainder) SplitFirst(ReadOnlySpan<byte> separator)
    {
        if (IsEmpty)
        {
            return default;
        }

        if (separator.IsEmpty)
        {
            return (this, default);
        }

        var span = AsSpan();
        var index = span.IndexOf(separator);
        return index >= 0
            ? (this[..index], this[(index + separator.Length)..])
            : (this, default);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (U8String Segment, U8String Remainder) SplitLast(byte separator)
    {
        if (!Rune.IsValid(separator))
        {
            // TODO: EH UX
            ThrowHelpers.ArgumentOutOfRange();
        }

        if (IsEmpty)
        {
            return default;
        }

        var span = AsSpan();
        var index = span.LastIndexOf(separator);
        return index >= 0
            ? (this[..index], this[(index + 1)..])
            : (this, default);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (U8String Segment, U8String Remainder) SplitLast(Rune separator)
    {
        if (IsEmpty)
        {
            return default;
        }

        var separatorBytes = (stackalloc byte[4]);
        var separatorLength = separator.EncodeToUtf8(separatorBytes);

        var span = AsSpan();
        var index = span.LastIndexOf(separatorBytes[..separatorLength]);
        return index >= 0
            ? (this[..index], this[(index + separatorLength)..])
            : (this, default);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (U8String Segment, U8String Remainder) SplitLast(U8String separator)
    {
        if (IsEmpty)
        {
            return default;
        }

        if (separator.IsEmpty)
        {
            return (this, default);
        }

        var span = AsSpan();
        var index = span.LastIndexOf(separator);
        return index >= 0
            ? (this[..index], this[(index + 1)..])
            : (this, default);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (U8String Segment, U8String Remainder) SplitLast(ReadOnlySpan<byte> separator)
    {
        if (IsEmpty)
        {
            return default;
        }

        if (separator.IsEmpty)
        {
            return (this, default);
        }

        var span = AsSpan();
        var index = span.LastIndexOf(separator);
        return index >= 0
            ? (this[..index], this[(index + separator.Length)..])
            : (this, default);
    }

    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public U8String Trim()
    // {
    //     if (IsEmpty || (
    //         !U8Info.IsWhitespaceSurrogate(FirstByte) &&
    //         !U8Info.IsWhitespaceSurrogate(IndexUnsafe(_length - 1))))
    //     {
    //         return this;
    //     }

    //     return TrimCore();
    // }

    /// <summary>
    /// Retrieves a substring from this instance. The substring starts at a specified
    /// character position and continues to the end of the string.
    /// </summary>
    /// <param name="startIndex">The zero-based starting character position of a substring in this instance.</param>
    /// <returns>A substring view that begins at <paramref name="startIndex"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String Substring(int startIndex) => this[startIndex..];

    /// <summary>
    /// Retrieves a substring from this instance. The substring starts at a specified
    /// character position and has a specified length.
    /// </summary>
    /// <param name="startIndex">The zero-based starting character position of a substring in this instance.</param>
    /// <param name="length">The number of bytes in the substring.</param>
    /// <returns>A substring view that begins at <paramref name="startIndex"/> and has <paramref name="length"/> bytes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String Substring(int startIndex, int length)
    {
        return this[startIndex..(startIndex + length)];
    }

    /// <summary>
    /// Removes all leading and trailing ASCII white-space characters from the current string.
    /// </summary>
    /// <returns>
    /// A substring that remains after all ASCII white-space characters
    /// are removed from the start and end of the current string.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String TrimAscii() => this[Ascii.Trim(this)];

    /// <summary>
    /// Removes all the leading ASCII white-space characters from the current string.
    /// </summary>
    /// <returns>
    /// A substring that remains after all white-space characters
    /// are removed from the start of the current string.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String TrimStartAscii() => this[Ascii.TrimStart(this)];

    /// <summary>
    /// Removes all the trailing ASCII white-space characters from the current string.
    /// </summary>
    /// <returns>
    /// A substring that remains after all white-space characters
    /// are removed from the end of the current string.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String TrimEndAscii() => this[Ascii.TrimEnd(this)];

    /// <summary>
    /// Returns a copy of this ASCII string converted to lower case.
    /// </summary>
    /// <returns>A lowercase equivalent of the current ASCII string.</returns>
    /// <exception cref="ArgumentException">
    /// The current string is not a valid ASCII sequence.
    /// </exception>
    public U8String ToLowerAscii()
    {
        if (IsEmpty)
        {
            return default;
        }

        var source = AsSpan();
        var destination = new byte[source.Length];
        var result = Ascii.ToLower(source, destination, out _);
        if (result is OperationStatus.InvalidData)
        {
            ThrowHelpers.InvalidAscii();
        }

        return new U8String(destination, 0, (uint)source.Length);
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
        if (IsEmpty)
        {
            return default;
        }

        var source = AsSpan();
        var destination = new byte[source.Length];
        var result = Ascii.ToUpper(source, destination, out _);
        if (result is OperationStatus.InvalidData)
        {
            ThrowHelpers.InvalidAscii();
        }

        return new U8String(destination, 0, (uint)source.Length);
    }

    // private U8String TrimCore()
    // {
    //     var span = AsSpan();
    //     var start = 0;
    //     while (start < span.Length && span[start].IsWhitespace())
    //     {
    //         start++;
    //     }

    //     var end = (int)(_length - 1);
    //     while (end >= start && span[end].IsWhitespace())
    //     {
    //         end--;
    //     }

    //     return end - start > 0
    //         ? this[start..++end]
    //         : default;
    // }
}
