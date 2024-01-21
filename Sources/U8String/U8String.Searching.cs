using System.Diagnostics;
using System.Text;

using U8.Abstractions;
using U8.Primitives;
using U8.Shared;

namespace U8;

// Use braces, ternary, etc. Why: explicit control over block layout and return merging with nice syntax.
#pragma warning disable RCS1003, RCS1179, IDE0045
public readonly partial struct U8String
{
    /// <summary>
    /// Calculates the number of occurrences of the specified <see cref="byte"/>
    /// in the current <see cref="U8String"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Count(byte value)
    {
        return !IsEmpty ? U8Searching.Count(UnsafeSpan, value) : 0;
    }

    /// <summary>
    /// Calculates the number of occurrences of the specified <see cref="char"/>
    /// in the current <see cref="U8String"/>.
    /// </summary>
    public int Count(char value)
    {
        ThrowHelpers.CheckSurrogate(value);

        return !IsEmpty ? U8Searching.Count(UnsafeSpan, value) : 0;
    }

    /// <summary>
    /// Calculates the number of occurrences of the specified <see cref="Rune"/>
    /// in the current <see cref="U8String"/>.
    /// </summary>
    public int Count(Rune value)
    {
        return !IsEmpty ? U8Searching.Count(UnsafeSpan, value) : 0;
    }

    /// <summary>
    /// Calculates the number of occurrences of the specified byte sequence
    /// in the current <see cref="U8String"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Count(ReadOnlySpan<byte> value)
    {
        return !IsEmpty ? U8Searching.Count(UnsafeSpan, value) : 0;
    }

    /// <summary>
    /// Calculates the length of the common prefix between
    /// this <see cref="U8String"/> and <paramref name="other"/>.
    /// </summary>
    public int CommonPrefixLength(U8String other)
    {
        if (!other.IsEmpty)
        {
            return CommonPrefixLength(other.UnsafeSpan);
        }

        return 0;
    }

    /// <summary>
    /// Calculates the length of the common prefix between
    /// this <see cref="U8String"/> and <paramref name="other"/>.
    /// </summary>
    public int CommonPrefixLength(ReadOnlySpan<byte> other)
    {
        var deref = this;
        if (!deref.IsEmpty)
        {
            return deref.UnsafeSpan.CommonPrefixLength(other);
        }

        return 0;
    }

    /// <summary>
    /// Indicates whether specified <see cref="byte"/> occurs within
    /// current <see cref="U8String"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(byte value)
    {
        return !IsEmpty && UnsafeSpan.Contains(value);
    }

    /// <summary>
    /// Indicates whether specified <see cref="char"/> occurs within
    /// current <see cref="U8String"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> is a surrogate character.
    /// Separate surrogate UTF-16 code units are not representable in UTF-8.
    /// </exception>
    public bool Contains(char value)
    {
        ThrowHelpers.CheckSurrogate(value);

        return !IsEmpty && U8Searching.Contains(value, ref UnsafeRef, Length);
    }

    /// <summary>
    /// Indicates whether specified <see cref="Rune"/> occurs within
    /// current <see cref="U8String"/>.
    /// </summary>
    public bool Contains(Rune value)
    {
        return !IsEmpty && U8Searching.Contains(value, ref UnsafeRef, Length);
    }

    /// <summary>
    /// Indicates whether specified <paramref name="value"/> occurs within
    /// current <see cref="U8String"/>.
    /// </summary>
    public bool Contains(U8String value)
    {
        bool result;
        var deref = this;
        if (!value.IsEmpty)
        {
            if (!deref.IsEmpty)
            {
                result = deref.UnsafeSpan.IndexOf(value.UnsafeSpan) >= 0;
            }
            else result = false;
        }
        else result = true;

        return result;
    }

    /// <summary>
    /// Indicates whether specified <paramref name="value"/> occurs within
    /// current <see cref="U8String"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(ReadOnlySpan<byte> value)
    {
        var deref = this;
        if (!deref.IsEmpty)
        {
            var span = deref.UnsafeSpan;
            return value.Length is 1
                ? span.Contains(value.AsRef())
                : span.IndexOf(value) >= 0;
        }

        return value.IsEmpty;
    }

    /// <summary>
    /// Indicates whether specified <see cref="byte"/> occurs within
    /// current <see cref="U8String"/> using specified <paramref name="comparer"/>.
    /// </summary>
    public bool Contains<T>(byte value, T comparer)
        where T : IU8ContainsOperator
    {
        return comparer.Contains(this, value);
    }

    /// <summary>
    /// Indicates whether specified <see cref="char"/> occurs within
    /// current <see cref="U8String"/> using specified <paramref name="comparer"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> is a surrogate character.
    /// Separate surrogate UTF-16 code units are not representable in UTF-8.
    /// </exception>
    public bool Contains<T>(char value, T comparer)
        where T : IU8ContainsOperator
    {
        ThrowHelpers.CheckSurrogate(value);

        return char.IsAscii(value)
            ? Contains((byte)value, comparer)
            : Contains(new U8Scalar(value, checkAscii: false).AsSpan(), comparer);
    }

    /// <summary>
    /// Indicates whether specified <see cref="Rune"/> occurs within
    /// current <see cref="U8String"/> using specified <paramref name="comparer"/>.
    /// </summary>
    public bool Contains<T>(Rune value, T comparer)
        where T : IU8ContainsOperator
    {
        return value.IsAscii
            ? Contains((byte)value.Value, comparer)
            : Contains(new U8Scalar(value, checkAscii: false).AsSpan(), comparer);
    }

    public bool Contains<T>(U8String value, T comparer)
        where T : IU8ContainsOperator
    {
        return comparer.Contains(this, value);
    }

    /// <summary>
    /// Indicates whether specified <paramref name="value"/> occurs within
    /// current <see cref="U8String"/> using specified <paramref name="comparer"/>.
    /// </summary>
    public bool Contains<T>(ReadOnlySpan<byte> value, T comparer)
        where T : IU8ContainsOperator
    {
        return comparer.Contains(this, value);
    }

    /// <summary>
    /// Indicates whether current <see cref="U8String"/> starts with
    /// specified <see cref="byte"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool StartsWith(byte value)
    {
        var (arr, offset) = (_value, Offset);
        return arr != null && arr.AsRef(offset) == value;
    }

    /// <summary>
    /// Indicates whether current <see cref="U8String"/> starts with
    /// specified <see cref="char"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> is a surrogate character.
    /// Separate surrogate UTF-16 code units are not representable in UTF-8.
    /// </exception>
    public bool StartsWith(char value)
    {
        ThrowHelpers.CheckSurrogate(value);

        return char.IsAscii(value)
            ? StartsWith((byte)value)
            : StartsWith(value <= 0x7FF ? value.AsTwoBytes() : value.AsThreeBytes());
    }

    /// <summary>
    /// Indicates whether current <see cref="U8String"/> starts with
    /// specified <see cref="Rune"/>.
    /// </summary>
    public bool StartsWith(Rune value)
    {
        return value.IsAscii
            ? StartsWith((byte)value.Value)
            : StartsWith(value.Value switch
            {
                <= 0x7FF => value.AsTwoBytes(),
                <= 0xFFFF => value.AsThreeBytes(),
                _ => value.AsFourBytes()
            });
    }

    /// <summary>
    /// Indicates whether current <see cref="U8String"/> starts with
    /// specified <paramref name="value"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool StartsWith(U8String value)
    {
        bool result;
        var deref = this;
        if (!value.IsEmpty)
        {
            if (value.Length <= deref.Length)
            {
                result = deref.UnsafeSpan.StartsWith(value.UnsafeSpan);
            }
            else result = false;
        }
        else result = true;

        return result;
    }

    /// <summary>
    /// Indicates whether current <see cref="U8String"/> starts with
    /// specified <paramref name="value"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool StartsWith(ReadOnlySpan<byte> value)
    {
        bool result;
        var deref = this;
        if (value.Length > 0)
        {
            if (value.Length <= deref.Length)
            {
                result = deref.UnsafeSpan.StartsWith(value);
            }
            else result = false;
        }
        else result = true;

        return result;
    }

    /// <summary>
    /// Indicates whether current <see cref="U8String"/> starts with
    /// specified <see cref="byte"/> using specified <paramref name="comparer"/>.
    /// </summary>
    public bool StartsWith<T>(byte value, T comparer)
        where T : IU8StartsWithOperator
    {
        return comparer.StartsWith(this, value);
    }

    /// <summary>
    /// Indicates whether current <see cref="U8String"/> starts with
    /// specified <see cref="char"/> using specified <paramref name="comparer"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> is a surrogate character.
    /// Separate surrogate UTF-16 code units are not representable in UTF-8.
    /// </exception>
    public bool StartsWith<T>(char value, T comparer)
        where T : IU8StartsWithOperator
    {
        ThrowHelpers.CheckSurrogate(value);

        return char.IsAscii(value)
            ? StartsWith((byte)value, comparer)
            : StartsWith(new U8Scalar(value, checkAscii: false).AsSpan(), comparer);
    }

    /// <summary>
    /// Indicates whether current <see cref="U8String"/> starts with
    /// specified <see cref="Rune"/> using specified <paramref name="comparer"/>.
    /// </summary>
    public bool StartsWith<T>(Rune value, T comparer)
        where T : IU8StartsWithOperator
    {
        return value.IsAscii
            ? StartsWith((byte)value.Value, comparer)
            : StartsWith(new U8Scalar(value, checkAscii: false).AsSpan(), comparer);
    }

    /// <summary>
    /// Indicates whether current <see cref="U8String"/> starts with
    /// specified <paramref name="value"/> using specified <paramref name="comparer"/>.
    /// </summary>
    public bool StartsWith<T>(U8String value, T comparer)
        where T : IU8StartsWithOperator
    {
        return comparer.StartsWith(this, value);
    }

    /// <summary>
    /// Indicates whether current <see cref="U8String"/> starts with
    /// specified <paramref name="value"/> using specified <paramref name="comparer"/>.
    /// </summary>
    public bool StartsWith<T>(ReadOnlySpan<byte> value, T comparer)
        where T : IU8StartsWithOperator
    {
        return comparer.StartsWith(this, value);
    }

    /// <summary>
    /// Indicates whether current <see cref="U8String"/> ends with
    /// specified <see cref="byte"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EndsWith(byte value)
    {
        var (arr, offset, length) = this;
        return arr != null && arr.AsRef(offset + length - 1) == value;
    }

    /// <summary>
    /// Indicates whether current <see cref="U8String"/> ends with
    /// specified <see cref="char"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> is a surrogate character.
    /// Separate surrogate UTF-16 code units are not representable in UTF-8.
    /// </exception>
    public bool EndsWith(char value)
    {
        ThrowHelpers.CheckSurrogate(value);

        return char.IsAscii(value)
            ? EndsWith((byte)value)
            : EndsWith(value <= 0x7FF ? value.AsTwoBytes() : value.AsThreeBytes());
    }

    /// <summary>
    /// Indicates whether current <see cref="U8String"/> ends with
    /// specified <see cref="Rune"/>.
    /// </summary>
    public bool EndsWith(Rune value) => value.IsAscii
        ? EndsWith((byte)value.Value)
        : EndsWith(value.Value switch
        {
            <= 0x7FF => value.AsTwoBytes(),
            <= 0xFFFF => value.AsThreeBytes(),
            _ => value.AsFourBytes()
        });

    /// <summary>
    /// Indicates whether current <see cref="U8String"/> ends with
    /// specified <paramref name="value"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EndsWith(U8String value)
    {
        bool result;
        var deref = this;
        if (!value.IsEmpty)
        {
            if (value.Length <= deref.Length)
            {
                result = deref.UnsafeSpan.EndsWith(value.UnsafeSpan);
            }
            else result = false;
        }
        else result = true;

        return result;
    }

    /// <summary>
    /// Indicates whether current <see cref="U8String"/> ends with
    /// specified <paramref name="value"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool EndsWith(ReadOnlySpan<byte> value)
    {
        bool result;
        var deref = this;
        if (value.Length > 0)
        {
            if (value.Length <= deref.Length)
            {
                result = deref.UnsafeSpan.EndsWith(value);
            }
            else result = false;
        }
        else result = true;

        return result;
    }

    /// <summary>
    /// Indicates whether current <see cref="U8String"/> ends with
    /// specified <see cref="byte"/> using specified <paramref name="comparer"/>.
    /// </summary>
    public bool EndsWith<T>(byte value, T comparer)
        where T : IU8EndsWithOperator
    {
        return comparer.EndsWith(this, value);
    }

    /// <summary>
    /// Indicates whether current <see cref="U8String"/> ends with
    /// specified <see cref="char"/> using specified <paramref name="comparer"/>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> is a surrogate character.
    /// Separate surrogate UTF-16 code units are not representable in UTF-8.
    /// </exception>
    public bool EndsWith<T>(char value, T comparer)
        where T : IU8EndsWithOperator
    {
        ThrowHelpers.CheckSurrogate(value);

        return char.IsAscii(value)
            ? EndsWith((byte)value, comparer)
            : EndsWith(new U8Scalar(value, checkAscii: false).AsSpan(), comparer);
    }

    /// <summary>
    /// Indicates whether current <see cref="U8String"/> ends with
    /// specified <see cref="Rune"/> using specified <paramref name="comparer"/>.
    /// </summary>
    public bool EndsWith<T>(Rune value, T comparer)
        where T : IU8EndsWithOperator
    {
        return value.IsAscii
            ? EndsWith((byte)value.Value, comparer)
            : EndsWith(new U8Scalar(value, checkAscii: false).AsSpan(), comparer);
    }

    /// <summary>
    /// Indicates whether current <see cref="U8String"/> ends with
    /// specified <paramref name="value"/> using specified <paramref name="comparer"/>.
    /// </summary>
    public bool EndsWith<T>(U8String value, T comparer)
        where T : IU8EndsWithOperator
    {
        return comparer.EndsWith(this, value);
    }

    /// <summary>
    /// Indicates whether current <see cref="U8String"/> ends with
    /// specified <paramref name="value"/> using specified <paramref name="comparer"/>.
    /// </summary>
    public bool EndsWith<T>(ReadOnlySpan<byte> value, T comparer)
        where T : IU8EndsWithOperator
    {
        return comparer.EndsWith(this, value);
    }

    /// <summary>
    /// Finds the first occurrence of the specified <see cref="byte"/>
    /// in the current <see cref="U8String"/>.
    /// </summary>
    /// <returns>
    /// The zero-based index of <paramref name="value"/> if it is found;
    /// <c>-1</c> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOf(byte value)
    {
        return !IsEmpty ? UnsafeSpan.IndexOf(value) : -1;
    }

    /// <summary>
    /// Finds the first occurrence of the specified <see cref="char"/>
    /// in the current <see cref="U8String"/>.
    /// </summary>
    /// <returns>
    /// The zero-based index of <paramref name="value"/> if it is found;
    /// <c>-1</c> otherwise.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> is a surrogate character.
    /// Separate surrogate UTF-16 code units are not representable in UTF-8.
    /// </exception>
    public int IndexOf(char value)
    {
        ThrowHelpers.CheckSurrogate(value);

        return char.IsAscii(value)
            ? IndexOf((byte)value)
            : IndexOfRune(value <= 0x7FF ? value.AsTwoBytes() : value.AsThreeBytes());
    }

    /// <summary>
    /// Finds the first occurrence of the specified <see cref="Rune"/>
    /// in the current <see cref="U8String"/>.
    /// </summary>
    /// <returns>
    /// The zero-based index of <paramref name="value"/> if it is found;
    /// <c>-1</c> otherwise.
    /// </returns>
    public int IndexOf(Rune value)
    {
        return value.IsAscii
            ? IndexOf((byte)value.Value)
            : IndexOfRune(value.Value switch
            {
                <= 0x7FF => value.AsTwoBytes(),
                <= 0xFFFF => value.AsThreeBytes(),
                _ => value.AsFourBytes()
            });
    }

    int IndexOfRune(ReadOnlySpan<byte> value)
    {
        Debug.Assert(value.Length is >= 2 and <= 4);

        var deref = this;
        return !deref.IsEmpty ? deref.UnsafeSpan.IndexOf(value) : -1;
    }

    /// <summary>
    /// Finds the first occurrence of the specified <paramref name="value"/>
    /// in the current <see cref="U8String"/>.
    /// </summary>
    /// <returns>
    /// The zero-based index of <paramref name="value"/> if it is found;
    /// <c>-1</c> otherwise.
    /// </returns>
    public int IndexOf(U8String value)
    {
        int result;
        var deref = this;
        if (!value.IsEmpty)
        {
            if (!deref.IsEmpty)
            {
                result = deref.UnsafeSpan.IndexOf(value.UnsafeSpan);
            }
            else result = -1;
        }
        else result = 0;

        return result;
    }

    /// <summary>
    /// Finds the first occurrence of the specified <paramref name="value"/>
    /// in the current <see cref="U8String"/>.
    /// </summary>
    /// <returns>
    /// The zero-based index of <paramref name="value"/> if it is found;
    /// <c>-1</c> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOf(ReadOnlySpan<byte> value)
    {
        int result;
        var deref = this;
        if (!deref.IsEmpty)
        {
            var span = deref.UnsafeSpan;
            result = value.Length is 1
                ? span.IndexOf(value.AsRef())
                : span.IndexOf(value);
        }
        else result = value.IsEmpty ? 0 : -1;

        return result;
    }

    /// <summary>
    /// Finds the first occurrence of the specified <see cref="byte"/>
    /// in the current <see cref="U8String"/> using specified <paramref name="comparer"/>.
    /// </summary>
    /// <returns>
    /// The zero-based index of <paramref name="value"/> if it is found;
    /// <c>-1</c> otherwise.
    /// </returns>
    public int IndexOf<T>(byte value, T comparer)
        where T : IU8IndexOfOperator
    {
        return comparer.IndexOf(this, value).Offset;
    }

    /// <summary>
    /// Finds the first occurrence of the specified <see cref="char"/>
    /// in the current <see cref="U8String"/> using specified <paramref name="comparer"/>.
    /// </summary>
    /// <returns>
    /// The zero-based index of <paramref name="value"/> if it is found;
    /// <c>-1</c> otherwise.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> is a surrogate character.
    /// Separate surrogate UTF-16 code units are not representable in UTF-8.
    /// </exception>
    public int IndexOf<T>(char value, T comparer)
        where T : IU8IndexOfOperator
    {
        ThrowHelpers.CheckSurrogate(value);

        return char.IsAscii(value)
            ? IndexOf((byte)value, comparer)
            : IndexOf(new U8Scalar(value, checkAscii: false).AsSpan(), comparer);
    }

    /// <summary>
    /// Finds the first occurrence of the specified <see cref="Rune"/>
    /// in the current <see cref="U8String"/> using specified <paramref name="comparer"/>.
    /// </summary>
    /// <returns>
    /// The zero-based index of <paramref name="value"/> if it is found;
    /// <c>-1</c> otherwise.
    /// </returns>
    public int IndexOf<T>(Rune value, T comparer)
        where T : IU8IndexOfOperator
    {
        return value.IsAscii
            ? IndexOf((byte)value.Value, comparer)
            : IndexOf(new U8Scalar(value, checkAscii: false).AsSpan(), comparer);
    }

    /// <summary>
    /// Finds the first occurrence of the specified <paramref name="value"/>
    /// in the current <see cref="U8String"/> using specified <paramref name="comparer"/>.
    /// </summary>
    /// <returns>
    /// The zero-based index of <paramref name="value"/> if it is found;
    /// <c>-1</c> otherwise.
    /// </returns>
    public int IndexOf<T>(U8String value, T comparer)
        where T : IU8IndexOfOperator
    {
        return comparer.IndexOf(this, value).Offset;
    }

    /// <summary>
    /// Finds the first occurrence of the specified <paramref name="value"/>
    /// in the current <see cref="U8String"/> using specified <paramref name="comparer"/>.
    /// </summary>
    /// <returns>
    /// The zero-based index of <paramref name="value"/> if it is found;
    /// <c>-1</c> otherwise.
    /// </returns>
    public int IndexOf<T>(ReadOnlySpan<byte> value, T comparer)
        where T : IU8IndexOfOperator
    {
        return comparer.IndexOf(this, value).Offset;
    }

    /// <summary>
    /// Finds the last occurrence of the specified <see cref="byte"/>
    /// in the current <see cref="U8String"/>.
    /// </summary>
    /// <returns>
    /// The zero-based index of <paramref name="value"/> if it is found;
    /// <c>-1</c> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int LastIndexOf(byte value)
    {
        return !IsEmpty ? UnsafeSpan.LastIndexOf(value) : -1;
    }

    /// <summary>
    /// Finds the last occurrence of the specified <see cref="char"/>
    /// in the current <see cref="U8String"/>.
    /// </summary>
    /// <returns>
    /// The zero-based index of <paramref name="value"/> if it is found;
    /// <c>-1</c> otherwise.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> is a surrogate character.
    /// Separate surrogate UTF-16 code units are not representable in UTF-8.
    /// </exception>
    public int LastIndexOf(char value)
    {
        ThrowHelpers.CheckSurrogate(value);

        return char.IsAscii(value)
            ? LastIndexOf((byte)value)
            : LastIndexOfRune(value <= 0x7FF ? value.AsTwoBytes() : value.AsThreeBytes());
    }

    /// <summary>
    /// Finds the last occurrence of the specified <see cref="Rune"/>
    /// in the current <see cref="U8String"/>.
    /// </summary>
    /// <returns>
    /// The zero-based index of <paramref name="value"/> if it is found;
    /// <c>-1</c> otherwise.
    /// </returns>
    public int LastIndexOf(Rune value)
    {
        return value.IsAscii
            ? LastIndexOf((byte)value.Value)
            : LastIndexOfRune(value.Value switch
            {
                <= 0x7FF => value.AsTwoBytes(),
                <= 0xFFFF => value.AsThreeBytes(),
                _ => value.AsFourBytes()
            });
    }

    int LastIndexOfRune(ReadOnlySpan<byte> value)
    {
        Debug.Assert(value.Length is >= 2 and <= 4);

        var deref = this;
        return !deref.IsEmpty ? deref.UnsafeSpan.LastIndexOf(value) : -1;
    }

    /// <summary>
    /// Finds the last occurrence of the specified <paramref name="value"/>
    /// in the current <see cref="U8String"/>.
    /// </summary>
    /// <returns>
    /// The zero-based index of <paramref name="value"/> if it is found;
    /// <c>-1</c> otherwise.
    /// </returns>
    public int LastIndexOf(U8String value)
    {
        int result;
        var deref = this;
        if (!value.IsEmpty)
        {
            if (!deref.IsEmpty)
            {
                result = deref.UnsafeSpan.LastIndexOf(value.UnsafeSpan);
            }
            else result = -1;
        }
        else result = 0;

        return result;
    }

    /// <summary>
    /// Finds the last occurrence of the specified <paramref name="value"/>
    /// in the current <see cref="U8String"/>.
    /// </summary>
    /// <returns>
    /// The zero-based index of <paramref name="value"/> if it is found;
    /// <c>-1</c> otherwise.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int LastIndexOf(ReadOnlySpan<byte> value)
    {
        int result;
        var deref = this;
        if (!deref.IsEmpty)
        {
            var span = deref.UnsafeSpan;
            result = value.Length is 1
                ? span.LastIndexOf(value.AsRef())
                : span.LastIndexOf(value);
        }
        else result = value.IsEmpty ? 0 : -1;

        return result;
    }

    /// <summary>
    /// Finds the last occurrence of the specified <see cref="byte"/>
    /// in the current <see cref="U8String"/> using specified <paramref name="comparer"/>.
    /// </summary>
    /// <returns>
    /// The index of <paramref name="value"/> if it is found;
    /// <c>-1</c> otherwise.
    /// </returns>
    public int LastIndexOf<T>(byte value, T comparer)
        where T : IU8LastIndexOfOperator
    {
        return comparer.LastIndexOf(this, value).Offset;
    }

    /// <summary>
    /// Finds the last occurrence of the specified <see cref="char"/>
    /// in the current <see cref="U8String"/> using specified <paramref name="comparer"/>.
    /// </summary>
    /// <returns>
    /// The index of <paramref name="value"/> if it is found;
    /// <c>-1</c> otherwise.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value"/> is a surrogate character.
    /// Separate surrogate UTF-16 code units are not representable in UTF-8.
    /// </exception>
    public int LastIndexOf<T>(char value, T comparer)
        where T : IU8LastIndexOfOperator
    {
        ThrowHelpers.CheckSurrogate(value);

        return char.IsAscii(value)
            ? LastIndexOf((byte)value, comparer)
            : LastIndexOf(new U8Scalar(value, checkAscii: false).AsSpan(), comparer);
    }

    /// <summary>
    /// Finds the last occurrence of the specified <see cref="Rune"/>
    /// in the current <see cref="U8String"/> using specified <paramref name="comparer"/>.
    /// </summary>
    /// <returns>
    /// The index of <paramref name="value"/> if it is found;
    /// <c>-1</c> otherwise.
    /// </returns>
    public int LastIndexOf<T>(Rune value, T comparer)
        where T : IU8LastIndexOfOperator
    {
        return value.IsAscii
            ? LastIndexOf((byte)value.Value, comparer)
            : LastIndexOf(new U8Scalar(value, checkAscii: false).AsSpan(), comparer);
    }

    /// <summary>
    /// Finds the last occurrence of the specified <paramref name="value"/>
    /// in the current <see cref="U8String"/> using specified <paramref name="comparer"/>.
    /// </summary>
    /// <returns>
    /// The index of <paramref name="value"/> if it is found;
    /// <c>-1</c> otherwise.
    /// </returns>
    public int LastIndexOf<T>(U8String value, T comparer)
        where T : IU8LastIndexOfOperator
    {
        return comparer.LastIndexOf(this, value).Offset;
    }

    /// <summary>
    /// Finds the last occurrence of the specified <paramref name="value"/>
    /// in the current <see cref="U8String"/> using specified <paramref name="comparer"/>.
    /// </summary>
    /// <returns>
    /// The index of <paramref name="value"/> if it is found;
    /// <c>-1</c> otherwise.
    /// </returns>
    public int LastIndexOf<T>(ReadOnlySpan<byte> value, T comparer)
        where T : IU8LastIndexOfOperator
    {
        return comparer.LastIndexOf(this, value).Offset;
    }
}
