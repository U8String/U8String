using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

using U8.Abstractions;
using U8.InteropServices;
using U8.Primitives;
using U8.Shared;

namespace U8;

#pragma warning disable IDE0046, IDE0057, RCS1003 // Why: range slicing and ternary expressions do not produce desired codegen
public readonly partial struct U8String
{
    /// <summary>
    /// Creates a new <see cref="U8String"/> from <paramref name="left"/> and <paramref name="right"/> appended together.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="right"/> is not an ASCII byte.
    /// </exception>
    public static U8String Concat(U8String left, byte right)
    {
        ThrowHelpers.CheckAscii(right);

        return U8Manipulation.ConcatUnchecked(left, right);
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from <paramref name="left"/> and <paramref name="right"/> appended together.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="right"/> is a surrogate character.
    /// </exception>
    public static U8String Concat(U8String left, char right)
    {
        ThrowHelpers.CheckSurrogate(right);

        return char.IsAscii(right)
            ? U8Manipulation.ConcatUnchecked(left, (byte)right)
            : U8Manipulation.ConcatUnchecked(left, new U8Scalar(right, checkAscii: false).AsSpan());
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from <paramref name="left"/> and <paramref name="right"/> appended together.
    /// </summary>
    public static U8String Concat(U8String left, Rune right)
    {
        return right.IsAscii
            ? U8Manipulation.ConcatUnchecked(left, (byte)right.Value)
            : U8Manipulation.ConcatUnchecked(left, new U8Scalar(right, checkAscii: false).AsSpan());
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from <paramref name="left"/> and <paramref name="right"/> appended together.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="left"/> is not an ASCII byte.
    /// </exception>
    public static U8String Concat(byte left, U8String right)
    {
        ThrowHelpers.CheckAscii(left);

        return U8Manipulation.ConcatUnchecked(left, right);
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from <paramref name="left"/> and <paramref name="right"/> appended together.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="left"/> is a surrogate character.
    /// </exception>
    public static U8String Concat(char left, U8String right)
    {
        ThrowHelpers.CheckSurrogate(left);

        return char.IsAscii(left)
            ? U8Manipulation.ConcatUnchecked((byte)left, right)
            : U8Manipulation.ConcatUnchecked(new U8Scalar(left, checkAscii: false).AsSpan(), right);
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from <paramref name="left"/> and <paramref name="right"/> appended together.
    /// </summary>
    public static U8String Concat(Rune left, U8String right)
    {
        return left.IsAscii
            ? U8Manipulation.ConcatUnchecked((byte)left.Value, right)
            : U8Manipulation.ConcatUnchecked(new U8Scalar(left, checkAscii: false).AsSpan(), right);
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from <paramref name="left"/> and <paramref name="right"/> appended together.
    /// </summary>
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

    /// <summary>
    /// Creates a new <see cref="U8String"/> from <paramref name="left"/> and <paramref name="right"/> appended together.
    /// </summary>
    /// <exception cref="FormatException">
    /// <paramref name="right"/> is not a valid UTF-8 byte sequence.
    /// </exception>
    public static U8String Concat(U8String left, ReadOnlySpan<byte> right)
    {
        if (right.Length > 0)
        {
            ValidatePossibleConstant(right);
            if (!left.IsEmpty)
            {
                return U8Manipulation.ConcatUnchecked(left.UnsafeSpan, right);
            }

            return new U8String(right, skipValidation: true);
        }

        return left;
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from <paramref name="left"/> and <paramref name="right"/> appended together.
    /// </summary>
    /// <exception cref="FormatException">
    /// <paramref name="left"/> is not a valid UTF-8 byte sequence.
    /// </exception>
    public static U8String Concat(ReadOnlySpan<byte> left, U8String right)
    {
        if (left.Length > 0)
        {
            ValidatePossibleConstant(left);
            if (!right.IsEmpty)
            {
                return U8Manipulation.ConcatUnchecked(left, right.UnsafeSpan);
            }

            return new U8String(left, skipValidation: true);
        }

        return right;
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from <paramref name="left"/> and <paramref name="right"/> appended together.
    /// </summary>
    /// <exception cref="FormatException">
    /// The resulting string is not a valid UTF-8 byte sequence.
    /// </exception>
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

    /// <summary>
    /// Concatenates the elements of a specified <see cref="U8String"/> array.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="values"/> is <see langword="null"/>.
    /// </exception>
    public static U8String Concat(U8String[] values)
    {
        ThrowHelpers.CheckNull(values);

        return Concat(values.AsSpan());
    }

    /// <summary>
    /// Concatenates the elements of a specified <see cref="ReadOnlySpan{T}"/> of <see cref="U8String"/> instances.
    /// </summary>
    public static U8String Concat(ReadOnlySpan<U8String> values)
    {
        if (values.Length > 1)
        {
            var llength = (long)0;
            foreach (var value in values)
            {
                llength += (uint)value.Length;
            }

            if (llength > 0)
            {
                var value = new byte[llength + 1];
                var length = (int)(uint)llength;

                var destination = value.AsSpan();
                foreach (var source in values)
                {
                    if (!source.IsEmpty)
                    {
                        // !Do not skip bounds check! because we cannot guarantee that
                        // the contents of the 'values' have not changed since the
                        // length was calculated. This matches the behavior of the
                        // string.Concat(params string[]) overload.
                        source.UnsafeSpan.CopyTo(destination);
                        destination = destination.SliceUnsafe(source.Length);
                    }
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

    /// <summary>
    /// Concatenates the elements of a specified <see cref="IEnumerable{T}"/> of <see cref="U8String"/> instances.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="values"/> is <see langword="null"/>.
    /// </exception>
    public static U8String Concat(IEnumerable<U8String> values)
    {
        ThrowHelpers.CheckNull(values);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Concat<T>(T values)
        where T : struct, IEnumerable<U8String>
    {
        return values switch
        {
            U8Lines lines => U8Manipulation.StripLineEndings(lines.Value),
            U8Slices slices => ConcatSlices(slices),
            U8Split split => split.Value.Remove(split.Separator),
            U8Split<byte> bsplit => bsplit.Value.Remove(bsplit.Separator),
            U8Split<char> csplit => csplit.Value.Remove(csplit.Separator),
            U8Split<Rune> rsplit => rsplit.Value.Remove(rsplit.Separator),
            ImmutableArray<U8String> array => Concat(array.AsSpan()),
            _ => Concat((IEnumerable<U8String>)values)
        };

        static U8String ConcatSlices(U8Slices slices)
        {
            if (slices.Count > 1)
            {
                var length = slices.Ranges!.TotalLength();
                if (length > 0)
                {
                    var bytes = new byte[(nint)(uint)length + 1];
                    var source = slices.Source;
                    ref var dst = ref bytes.AsRef();
                    foreach (var range in slices.Ranges!)
                    {
                        source!.SliceUnsafe(range.Offset, range.Length).CopyToUnsafe(ref dst);
                        dst = ref dst.Add(range.Length);
                    }
                }
            }
            else if (slices.Count is 1)
            {
                return slices[0];
            }

            return default;
        }
    }

    public static U8String Concat<T>(
        T[] values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        ThrowHelpers.CheckNull(values);

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
        ThrowHelpers.CheckNull(values);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(Span<byte> destination)
    {
        var source = this;
        if (!source.IsEmpty)
        {
            source.UnsafeSpan.CopyTo(destination);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryCopyTo(Span<byte> destination)
    {
        var source = this;
        var result = true;
        if (!source.IsEmpty)
        {
            if (destination.Length >= source.Length)
            {
                source.UnsafeSpan.CopyToUnsafe(ref destination.AsRef());
                result = true;
            }
            else result = false;
        }

        return result;
    }

    /// <inheritdoc />
    void ICollection<byte>.CopyTo(byte[] destination, int index)
    {
        ThrowHelpers.CheckNull(destination);

        AsSpan().CopyTo(destination.AsSpan()[index..]);
    }

    public static U8String Join(byte separator, U8String[] values)
    {
        ThrowHelpers.CheckNull(values);

        return Join(separator, values.AsSpan());
    }

    public static U8String Join(byte separator, ReadOnlySpan<U8String> values)
    {
        ThrowHelpers.CheckAscii(separator);

        return U8Manipulation.Join(separator, values);
    }

    public static U8String Join(byte separator, IEnumerable<U8String> values)
    {
        ThrowHelpers.CheckAscii(separator);

        return U8Manipulation.Join(separator, values);
    }

    public static U8String Join(char separator, U8String[] values)
    {
        ThrowHelpers.CheckNull(values);

        return Join(separator, values.AsSpan());
    }

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

    public static U8String Join(Rune separator, U8String[] values)
    {
        ThrowHelpers.CheckNull(values);

        return Join(separator, values.AsSpan());
    }

    public static U8String Join(Rune separator, ReadOnlySpan<U8String> values)
    {
        return separator.IsAscii
            ? U8Manipulation.Join((byte)separator.Value, values)
            : U8Manipulation.Join(new U8Scalar(separator, checkAscii: false).AsSpan(), values);
    }

    public static U8String Join(Rune separator, IEnumerable<U8String> values)
    {
        ThrowHelpers.CheckNull(values);

        return separator.IsAscii
            ? U8Manipulation.Join((byte)separator.Value, values)
            : U8Manipulation.Join(new U8Scalar(separator, checkAscii: false).AsSpan(), values);
    }

    public static U8String Join(ReadOnlySpan<byte> separator, U8String[] values)
    {
        ThrowHelpers.CheckNull(values);

        return Join(separator, values.AsSpan());
    }

    public static U8String Join(ReadOnlySpan<byte> separator, ReadOnlySpan<U8String> values)
    {
        ValidatePossibleConstant(separator);

        return U8Manipulation.Join(separator, values);
    }

    public static U8String Join(ReadOnlySpan<byte> separator, IEnumerable<U8String> values)
    {
        ValidatePossibleConstant(separator);

        return U8Manipulation.Join(separator, values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Join<T>(byte separator, T values)
        where T : struct, IEnumerable<U8String>
    {
        ThrowHelpers.CheckAscii(separator);

        return values switch
        {
            U8Lines lines => JoinLines(separator, lines),
            U8Slices slices => JoinSlices(separator, slices),
            U8Split split => JoinSplit(separator, split),
            U8Split<byte> bsplit => JoinBSplit(separator, bsplit),
            U8Split<char> csplit => JoinCSplit(separator, csplit),
            U8Split<Rune> rsplit => JoinRSplit(separator, rsplit),
            ConfiguredU8Split split => JoinSplitC(separator, split),
            ConfiguredU8Split<byte> bsplit => JoinBSplitC(separator, bsplit),
            ConfiguredU8Split<char> csplit => JoinCSplitC(separator, csplit),
            ConfiguredU8Split<Rune> rsplit => JoinRSplitC(separator, rsplit),
            ImmutableArray<U8String> array => Join(separator, array.AsSpan()),
            _ => Join(separator, (IEnumerable<U8String>)values)
        };

        // These work around inliner limitations and allow to have a graceful failure
        // mode when the inliner does run out of budget.
        static U8String JoinLines(byte separator, U8Lines lines) =>
            lines.Value.ReplaceLineEndings(separator);

        static U8String JoinSlices(byte separator, U8Slices slices) => slices.Count switch
        {
            > 1 => U8Manipulation.JoinSized<U8Slices, U8Slices.Enumerator>(
                separator, slices, slices.Ranges!.TotalLength()),
            1 => slices[0],
            _ => default,
        };

        static U8String JoinSplit(byte separator, U8Split split) =>
            U8Manipulation.ReplaceCore(split.Value, split.Separator, new Span<byte>(ref separator), validate: false);

        static U8String JoinBSplit(byte separator, U8Split<byte> split) =>
            U8Manipulation.Replace(split.Value, split.Separator, separator, validate: false);

        static U8String JoinCSplit(byte separator, U8Split<char> split) =>
            U8Manipulation.Replace(split.Value, split.Separator, (char)separator);

        static U8String JoinRSplit(byte separator, U8Split<Rune> split) =>
            U8Manipulation.Replace(split.Value, split.Separator, new Rune(separator));

        static U8String JoinSplitC(byte separator, ConfiguredU8Split split) =>
            U8Manipulation.Join<ConfiguredU8Split, ConfiguredU8Split.Enumerator>(separator, split);

        static U8String JoinBSplitC(byte separator, ConfiguredU8Split<byte> split) =>
            U8Manipulation.Join<ConfiguredU8Split<byte>, ConfiguredU8Split<byte>.Enumerator>(separator, split);

        static U8String JoinCSplitC(byte separator, ConfiguredU8Split<char> split) =>
            U8Manipulation.Join<ConfiguredU8Split<char>, ConfiguredU8Split<char>.Enumerator>(separator, split);

        static U8String JoinRSplitC(byte separator, ConfiguredU8Split<Rune> split) =>
            U8Manipulation.Join<ConfiguredU8Split<Rune>, ConfiguredU8Split<Rune>.Enumerator>(separator, split);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Join<T>(char separator, T values)
        where T : struct, IEnumerable<U8String>
    {
        ThrowHelpers.CheckSurrogate(separator);

        return char.IsAscii(separator)
            ? Join((byte)separator, values)
            : JoinSpan(new U8Scalar(separator, checkAscii: false).AsSpan(), values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Join<T>(Rune separator, T values)
        where T : struct, IEnumerable<U8String>
    {
        return separator.IsAscii
            ? Join((byte)separator.Value, values)
            : JoinSpan(new U8Scalar(separator, checkAscii: false).AsSpan(), values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Join<T>(ReadOnlySpan<byte> separator, T values)
        where T : struct, IEnumerable<U8String>
    {
        ValidatePossibleConstant(separator);

        return separator.Length switch
        {
            0 => Concat(values),
            1 => Join(separator[0], values),
            _ => JoinSpan(separator, values)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static U8String Join<T>(U8String separator, T values)
        where T : struct, IEnumerable<U8String>
    {
        return !separator.IsEmpty ? separator.Length switch
        {
            1 => Join(separator.UnsafeRef, values),
            _ => JoinSpan(separator, values)
        } : Concat(values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static U8String JoinSpan<T>(ReadOnlySpan<byte> separator, T values)
        where T : struct, IEnumerable<U8String>
    {
        Debug.Assert(separator.Length > 1);

        return values switch
        {
            U8Lines lines => JoinLines(separator, lines),
            U8Slices slices => JoinSlices(separator, slices),
            U8Split split => JoinSplit(separator, split),
            U8Split<byte> bsplit => JoinBSplit(separator, bsplit),
            U8Split<char> csplit => JoinCSplit(separator, csplit),
            U8Split<Rune> rsplit => JoinRSplit(separator, rsplit),
            ConfiguredU8Split split => JoinSplitC(separator, split),
            ConfiguredU8Split<byte> bsplit => JoinBSplitC(separator, bsplit),
            ConfiguredU8Split<char> csplit => JoinCSplitC(separator, csplit),
            ConfiguredU8Split<Rune> rsplit => JoinRSplitC(separator, rsplit),
            ImmutableArray<U8String> array => Join(separator, array.AsSpan()),
            _ => Join(separator, (IEnumerable<U8String>)values)
        };

        // These work around inliner limitations and allow to have a graceful failure
        // mode when the inliner does run out of budget.
        static U8String JoinLines(ReadOnlySpan<byte> separator, U8Lines lines) =>
            lines.Value.ReplaceLineEndings(separator);

        static U8String JoinSlices(ReadOnlySpan<byte> separator, U8Slices slices) => slices.Count switch
        {
            > 1 => U8Manipulation.JoinSized<U8Slices, U8Slices.Enumerator>(
                separator, slices, slices.Ranges!.TotalLength()),
            1 => slices[0],
            _ => default,
        };

        static U8String JoinSplit(ReadOnlySpan<byte> separator, U8Split split) =>
            U8Manipulation.ReplaceCore(split.Value, split.Separator, separator, validate: false);

        static U8String JoinBSplit(ReadOnlySpan<byte> separator, U8Split<byte> split) =>
            U8Manipulation.Replace(split.Value, new U8Scalar(split.Separator).AsSpan(), separator, validate: false);

        static U8String JoinCSplit(ReadOnlySpan<byte> separator, U8Split<char> split) =>
            U8Manipulation.Replace(split.Value, new U8Scalar(split.Separator).AsSpan(), separator, validate: false);

        static U8String JoinRSplit(ReadOnlySpan<byte> separator, U8Split<Rune> split) =>
            U8Manipulation.Replace(split.Value, new U8Scalar(split.Separator).AsSpan(), separator, validate: false);

        static U8String JoinSplitC(ReadOnlySpan<byte> separator, ConfiguredU8Split split) =>
            U8Manipulation.Join<ConfiguredU8Split, ConfiguredU8Split.Enumerator>(separator, split);

        static U8String JoinBSplitC(ReadOnlySpan<byte> separator, ConfiguredU8Split<byte> split) =>
            U8Manipulation.Join<ConfiguredU8Split<byte>, ConfiguredU8Split<byte>.Enumerator>(separator, split);

        static U8String JoinCSplitC(ReadOnlySpan<byte> separator, ConfiguredU8Split<char> split) =>
            U8Manipulation.Join<ConfiguredU8Split<char>, ConfiguredU8Split<char>.Enumerator>(separator, split);

        static U8String JoinRSplitC(ReadOnlySpan<byte> separator, ConfiguredU8Split<Rune> split) =>
            U8Manipulation.Join<ConfiguredU8Split<Rune>, ConfiguredU8Split<Rune>.Enumerator>(separator, split);
    }

    public static U8String Join(byte separator, U8Chars chars)
    {
        ThrowHelpers.CheckAscii(separator);

        return U8Manipulation.JoinRunes<U8Manipulation.CharsSource>(separator, chars.Value);
    }

    public static U8String Join(char separator, U8Chars chars)
    {
        ThrowHelpers.CheckSurrogate(separator);

        return char.IsAscii(separator)
            ? U8Manipulation.JoinRunes<U8Manipulation.CharsSource>((byte)separator, chars.Value)
            : JoinSpan<U8Manipulation.CharsSource>(new U8Scalar(separator, checkAscii: false).AsSpan(), chars.Value);
    }

    public static U8String Join(Rune separator, U8Chars chars)
    {
        return separator.IsAscii
            ? U8Manipulation.JoinRunes<U8Manipulation.CharsSource>((byte)separator.Value, chars.Value)
            : JoinSpan<U8Manipulation.CharsSource>(new U8Scalar(separator, checkAscii: false).AsSpan(), chars.Value);
    }

    public static U8String Join(ReadOnlySpan<byte> separator, U8Chars chars)
    {
        ValidatePossibleConstant(separator);

        return JoinSpan<U8Manipulation.CharsSource>(separator, chars.Value);
    }

    public static U8String Join(U8String separator, U8Chars chars)
    {
        return JoinSpan<U8Manipulation.CharsSource>(separator, chars.Value);
    }

    public static U8String Join(byte separator, U8Runes runes)
    {
        ThrowHelpers.CheckAscii(separator);

        return U8Manipulation.JoinRunes<U8Manipulation.RunesSource>(separator, runes.Value);
    }

    public static U8String Join(char separator, U8Runes runes)
    {
        ThrowHelpers.CheckSurrogate(separator);

        return char.IsAscii(separator)
            ? U8Manipulation.JoinRunes<U8Manipulation.RunesSource>((byte)separator, runes.Value)
            : JoinSpan<U8Manipulation.RunesSource>(new U8Scalar(separator, checkAscii: false).AsSpan(), runes.Value);
    }

    public static U8String Join(Rune separator, U8Runes runes)
    {
        return separator.IsAscii
            ? U8Manipulation.JoinRunes<U8Manipulation.RunesSource>((byte)separator.Value, runes.Value)
            : JoinSpan<U8Manipulation.RunesSource>(new U8Scalar(separator, checkAscii: false).AsSpan(), runes.Value);
    }

    public static U8String Join(ReadOnlySpan<byte> separator, U8Runes runes)
    {
        ValidatePossibleConstant(separator);

        return JoinSpan<U8Manipulation.RunesSource>(separator, runes.Value);
    }

    public static U8String Join(U8String separator, U8Runes runes)
    {
        return JoinSpan<U8Manipulation.RunesSource>(separator, runes.Value);
    }

    static U8String JoinSpan<TSource>(ReadOnlySpan<byte> separator, U8String value)
        where TSource : struct, U8Manipulation.IRunesSource
    {
        if (separator.Length > 0)
        {
            return separator.Length is 1
                ? U8Manipulation.JoinRunes<TSource>(separator[0], value)
                : U8Manipulation.JoinRunes<TSource>(separator, value);
        }

        return value;
    }

    public static U8String Join<T>(
        byte separator,
        T[] values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        ThrowHelpers.CheckNull(values);

        return Join<T>(separator, values.AsSpan(), format, provider);
    }

    public static U8String Join<T>(
        byte separator,
        ReadOnlySpan<T> values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        ThrowHelpers.CheckAscii(separator);

        return U8Manipulation.Join(separator, values, format, provider);
    }

    public static U8String Join<T>(
        byte separator,
        IEnumerable<T> values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        ThrowHelpers.CheckAscii(separator);
        ThrowHelpers.CheckNull(values);

        return U8Manipulation.Join(separator, values, format, provider);
    }

    public static U8String Join<T>(
        char separator,
        T[] values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        ThrowHelpers.CheckNull(values);

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
        T[] values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        ThrowHelpers.CheckNull(values);

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
        ThrowHelpers.CheckNull(values);

        return separator.IsAscii
            ? U8Manipulation.Join((byte)separator.Value, values, format, provider)
            : U8Manipulation.Join(new U8Scalar(separator, checkAscii: false).AsSpan(), values, format, provider);
    }

    public static U8String Join<T>(
        ReadOnlySpan<byte> separator,
        T[] values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        ThrowHelpers.CheckNull(values);

        return Join<T>(separator, values.AsSpan(), format, provider);
    }

    public static U8String Join<T>(
        ReadOnlySpan<byte> separator,
        ReadOnlySpan<T> values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        ValidatePossibleConstant(separator);

        return U8Manipulation.Join(separator, values, format, provider);
    }

    public static U8String Join<T>(
        ReadOnlySpan<byte> separator,
        IEnumerable<T> values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        ValidatePossibleConstant(separator);

        return U8Manipulation.Join(separator, values, format, provider);
    }

    /// <summary>
    /// Normalizes current <see cref="U8String"/> to the specified Unicode normalization form.
    /// </summary>
    /// <returns>A new <see cref="U8String"/> normalized to the specified form.</returns>
    internal U8String Normalize(NormalizationForm form = NormalizationForm.FormC)
    {
        if (!Enum.IsDefined(form))
        {
            ThrowHelpers.ArgumentOutOfRange(nameof(form));
        }

        var source = this;
        if (!source.IsEmpty)
        {
            var value = source.UnsafeSpan;
            var nonAsciiOffset = (int)Polyfills.Text.Ascii.GetIndexOfFirstNonAsciiByte(value);
            value = value.SliceUnsafe(nonAsciiOffset);

            if (value.Length > 0)
            {
                throw new NotImplementedException();
            }

            return source;
        }

        return default;
    }

#pragma warning disable RCS1179 // Unnecessary assignment of a value. Why: manually merge return block and improve jump threading
    /// <summary>
    /// Returns an explicitly null-terminated variant of the current <see cref="U8String"/>.
    /// </summary>
    /// <remarks>
    /// Most new instances of <see cref="U8String"/> are already implicitly null-terminated.
    /// Calling this method in such situations will return the original instance with its
    /// length adjusted to include the null terminator that is already present in the
    /// underlying buffer.
    /// <para/>
    /// If this instance of <see cref="U8String"/> is empty, this method will return <see cref="U8Constants.NullByte"/>.
    /// <para/>
    /// For <see cref="U8String"/> instances that are not null-terminated,
    /// a new copy will be created with the null terminator appended to the end.
    /// </remarks>
    public U8String NullTerminate()
    {
        var (value, offset, length) = this;

        U8String result;
        if (value != null)
        {
            var end = offset + length;
            // Same as IsNullTerminated - split AsRef and Add to skip debug assert
            ref var ptr = ref value.AsRef().Add(end);

            if ((uint)end < (uint)value.Length && ptr is 0)
            {
                result = new(value, offset, length + 1);
            }
            else if (ptr.Substract(1) is 0)
            {
                result = new(value, offset, length);
            }
            else
            {
                var bytes = new byte[(nint)(uint)(length + 1)];
                value.SliceUnsafe(offset, length).CopyToUnsafe(ref bytes.AsRef());
                result = new(bytes, 0, length + 1);
            }
        }
        else
        {
            result = U8Constants.NullByte;
        }

        return result;
    }
#pragma warning restore RCS1179

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
    public U8String ReplaceLineEndings(byte lineEnding)
    {
        ThrowHelpers.CheckAscii(lineEnding);

        if (!BitConverter.IsLittleEndian)
        {
            ThrowHelpers.NotSupportedBigEndian();
        }

        if (lineEnding is (byte)'\n')
        {
            return U8Manipulation.LineEndingsToLF(this);
        }

        return U8Manipulation.LineEndingsToCustom(this, lineEnding);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String ReplaceLineEndings(char lineEnding)
    {
        ThrowHelpers.CheckSurrogate(lineEnding);

        return char.IsAscii(lineEnding)
            ? ReplaceLineEndings((byte)lineEnding)
            : U8Manipulation.LineEndingsToCustom(this, new U8Scalar(lineEnding, checkAscii: false).AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String ReplaceLineEndings(Rune lineEnding)
    {
        return lineEnding.IsAscii
            ? ReplaceLineEndings((byte)lineEnding.Value)
            : U8Manipulation.LineEndingsToCustom(this, new U8Scalar(lineEnding, checkAscii: false).AsSpan());
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
            ValidatePossibleConstant(lineEnding);
            return U8Manipulation.LineEndingsToCustom(this, lineEnding);
        }
    }

    /// <summary>
    /// Forms a slice out of the current <see cref="U8String"/> instance starting at a specified index.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="start"/> is less than zero or greater than the length of this instance or
    /// when the resulting slice offsets point to the middle of a UTF-8 code point.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String Slice(int start)
    {
        var source = this;
        var length = (long)(uint)source.Length - (long)(uint)start;
        if (length < 0)
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        start += source.Offset;
        if (length > 0 && U8Info.IsContinuationByte(in source._value!.AsRef(start)))
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        return new(source._value, start, (int)(uint)(ulong)length);
    }

    /// <summary>
    /// Forms a slice out of the current <see cref="U8String"/> instance starting at a specified index for a specified length.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="start"/> is less than zero or greater than the length of this instance or
    /// when the resulting slice offsets point to the middle of a UTF-8 code point.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String Slice(int start, int length)
    {
        // The exact order of declaration and checks is very important because
        // this method is effectively a multi-optimization problem and falls
        // apart easily if you look at it funny.
        // When making changes, special care must be taken to ensure none of the
        // scenarios below have any regressions in both inlined and not inlined forms:
        // - Slice(0, n)
        // - Slice(n, str.Length - n)
        // - Slice(n1, n2)
        // - Slice(0, str.Length)
        // - enregisteredLocal.Slice(...)
        // - heap/stackReference.Slice(...)
        var source = this;
        if ((ulong)(uint)start + (ulong)(uint)length > (ulong)(uint)source.Length)
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        var offset = source.Offset;
        if (length > 0)
        {
            ref var ptr = ref source._value!.AsRef(offset += start);
            if ((start > 0 && U8Info
                    .IsContinuationByte(in ptr)) ||
                (length < (source.Length - start) && U8Info
                    .IsContinuationByte(in ptr.Add(length))))
            {
                ThrowHelpers.ArgumentOutOfRange();
            }
        }

        return new(source._value, offset, length);
    }

    /// <summary>
    /// Forms a slice out of the current <see cref="U8String"/> instance starting at a specified index.
    /// </summary>
    /// <remarks>
    /// For more information, see <see cref="SliceRounding(int,int)"/>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String SliceRounding(int start)
    {
        var source = this;
        var offset = source.Offset + (start > 0 ? Math.Min(start, source.Length) : 0);
        var newlength = source.Length - offset;
        if (newlength > 0)
        {
            ref var ptr = ref source._value!.AsRef(offset);
            if (start > 0)
            {
                var searchStart = 0;
                while (searchStart < newlength
                    && U8Info.IsContinuationByte(in ptr.Add(searchStart)))
                {
                    searchStart++;
                }
                offset += searchStart;
                newlength -= searchStart;
            }
        }

        return new(source._value, offset, newlength);
    }

    /// <summary>
    /// Forms a slice out of the current <see cref="U8String"/> instance starting at a specified index for a specified length.
    /// </summary>
    /// <remarks>
    /// In the instance where <paramref name="start"/> and/or <paramref name="length"/> are negative,
    /// out of range, or point to the middle of a UTF-8 code point, the resulting slice will have
    /// its start and/or end adjusted to the nearest valid UTF-8 code point boundaries rounding
    /// towards the middle of the string. This method will never throw and will always return a
    /// valid slice.
    /// <para />
    /// Example:
    /// <example>
    /// <code>
    /// var str = (U8String)"Привіт, Всесвіт!"u8;
    /// var slice = str.SliceRounding(1, 12);
    /// Assert.Equal("ривіт"u8, slice);
    ///
    /// var slice2 = str.SliceRounding(int.MinValue, int.MaxValue);
    /// Assert.Equal(str, slice2);
    /// </code>
    /// </example>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String SliceRounding(int start, int length)
    {
        var source = this;
        var offset = source.Offset + (start > 0 ? Math.Min(start, source.Length) : 0);
        var newlength = Math.Min(length, source.Length - offset);
        if (newlength > 0)
        {
            ref var ptr = ref source._value!.AsRef(offset);
            if (start > 0)
            {
                var searchStart = 0;
                while (searchStart < newlength
                    && U8Info.IsContinuationByte(in ptr.Add(searchStart)))
                {
                    searchStart++;
                }
                offset += searchStart;
                newlength -= searchStart;
            }

            if (length < (source.Length - start))
            {
                while (newlength > 0
                    && U8Info.IsContinuationByte(in ptr.Add(newlength)))
                {
                    newlength--;
                }
            }
        }

        return new(source._value, offset, newlength);
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
            var last = ptr.Add(source.Length - 1);

            if (U8Info.IsAsciiByte(in ptr) && !U8Info.IsAsciiWhitespace(in ptr) &&
                U8Info.IsAsciiByte(last) && !U8Info.IsAsciiWhitespace(last))
            {
                return source;
            }

            return TrimCore(source._value, source.Offset, source.Length);
        }

        return default;

        // The code below looks simple but it's surprisingly migraine-inducing
        static U8String TrimCore(byte[] source, int offset, int length)
        {
            ref var ptr = ref source.AsRef(offset);

            var index = 0;
            while (index < length)
            {
                if (!U8Info.IsWhitespaceRune(ref ptr.Add(index), out var size))
                {
                    break;
                }
                index += size;
            }

            ptr = ref ptr.Add(index);
            offset += index;
            length -= index;

            for (var endSearch = length - 1; endSearch >= 0; endSearch--)
            {
                var b = ptr.Add(endSearch);
                if (U8Info.IsBoundaryByte(b))
                {
                    if (U8Info.IsAsciiByte(b)
                        ? U8Info.IsAsciiWhitespace(b)
                        : U8Info.IsNonAsciiWhitespace(ref ptr.Add(endSearch), out _))
                    {
                        // Save the last found whitespace code point offset and continue searching
                        // for more whitspace byte sequences from their end. If we don't do this,
                        // we will end up trimming away continuation bytes at the end of the string.
                        length = endSearch;
                    }
                    else break;
                }
            }

            return new U8String(source, offset, length);
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

            return new(source._value, source.Offset + start, source.Length - start);
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
                if (U8Info.IsBoundaryByte(b))
                {
                    if (U8Info.IsAsciiByte(b)
                        ? U8Info.IsAsciiWhitespace(b)
                        : U8Info.IsNonAsciiWhitespace(ref ptr.Add(endSearch), out _))
                    {
                        end = endSearch - 1;
                    }
                    else break;
                }
            }

            return new(source._value, source.Offset, end + 1);
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String TrimAscii()
    {
        var source = this;

        return !source.IsEmpty
            ? U8Marshal.Slice(source, Ascii.TrimEnd(source.UnsafeSpan))
            : source;
    }

    /// <summary>
    /// Removes all the leading ASCII whitespace characters from the current string.
    /// </summary>
    /// <returns>
    /// A sub-slice that remains after all whitespace characters
    /// are removed from the start of the current string.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String TrimStartAscii()
    {
        var source = this;

        return !source.IsEmpty
            ? U8Marshal.Slice(source, Ascii.TrimStart(source.UnsafeSpan))
            : source;
    }

    /// <summary>
    /// Removes all the trailing ASCII whitespace characters from the current string.
    /// </summary>
    /// <returns>
    /// A sub-slice that remains after all whitespace characters
    /// are removed from the end of the current string.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String TrimEndAscii()
    {
        var source = this;

        return !source.IsEmpty
            ? U8Marshal.Slice(source, Ascii.TrimEnd(source.UnsafeSpan))
            : source;
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> with the characters converted to lowercase using specified <paramref name="converter"/>.
    /// </summary>
    /// <param name="converter">The case conversion implementation to use.</param>
    /// <typeparam name="T">The type of the case conversion implementation.</typeparam>
    /// <remarks>
    /// If there are no characters to convert, the current instance is returned instead.
    /// </remarks>
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

    /// <summary>
    /// Creates a new <see cref="U8String"/> with the characters converted to uppercase using specified <paramref name="converter"/>.
    /// </summary>
    /// <param name="converter">The case conversion implementation to use.</param>
    /// <typeparam name="T">The type of the case conversion implementation.</typeparam>
    /// <remarks>
    /// If there are no characters to convert, the current instance is returned instead.
    /// </remarks>
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

    /// <summary>
    /// Creates a new <see cref="U8String"/> with the ASCII characters converted to lowercase.
    /// </summary>
    public U8String ToLowerAscii()
    {
        return ToLower(U8CaseConversion.Ascii);
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> with the ASCII characters converted to uppercase.
    /// </summary>
    public U8String ToUpperAscii()
    {
        return ToUpper(U8CaseConversion.Ascii);
    }
}
