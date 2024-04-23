using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            : U8Manipulation.ConcatUnchecked(left, right <= 0x7FF ? right.AsTwoBytes() : right.AsThreeBytes());
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from <paramref name="left"/> and <paramref name="right"/> appended together.
    /// </summary>
    public static U8String Concat(U8String left, Rune right)
    {
        return right.IsAscii
            ? U8Manipulation.ConcatUnchecked(left, (byte)right.Value)
            : U8Manipulation.ConcatUnchecked(left, right.Value switch
            {
                <= 0x7FF => right.AsTwoBytes(),
                <= 0xFFFF => right.AsThreeBytes(),
                _ => right.AsFourBytes()
            });
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from <paramref name="left"/> and <paramref name="right"/> appended together.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="left"/> is not an ASCII byte.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            : U8Manipulation.ConcatUnchecked(
                left <= 0x7FF ? left.AsTwoBytes() : left.AsThreeBytes(), right);
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    /// <summary>
    /// Creates a new <see cref="U8String"/> from <paramref name="left"/> and <paramref name="right"/> appended together.
    /// </summary>
    /// <exception cref="FormatException">
    /// <paramref name="left"/> is not a valid UTF-8 byte sequence.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                    // !Do not skip bounds check! because we cannot guarantee that
                    // the contents of the 'values' have not changed since the
                    // length was calculated. This matches the behavior of the
                    // string.Concat(params string[]) overload.
                    source.AsSpan().CopyTo(destination);
                    destination = destination.SliceUnsafe(source.Length);
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

        if (values.TryGetSpan(out var span))
        {
            return Concat(span);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Concat<T>(
        T[] values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        ThrowHelpers.CheckNull(values);

        return Concat<T>(values.AsSpan(), format, provider);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Concat<T>(
        ReadOnlySpan<T> values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        // A few odd cases that may not be likely, but we can handle them for free so why not?
        if (typeof(T) == typeof(char))
        {
            return new(values.Cast<T, char>());
        }
        if (typeof(T) == typeof(Rune))
        {
            return U8Conversions.RunesToU8(values.Cast<T, Rune>());
        }
        else if (typeof(T) == typeof(U8String))
        {
            return Concat(values.Cast<T, U8String>());
        }

        provider ??= CultureInfo.InvariantCulture;
        if (values.Length != 1)
        {
            return ConcatSpan(values, format, provider);
        }

        return Create(values[0], format, provider);

        static U8String ConcatSpan(
            ReadOnlySpan<T> values,
            ReadOnlySpan<char> format,
            IFormatProvider provider)
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

    public static U8String Concat<T>(
        IEnumerable<T> values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        ThrowHelpers.CheckNull(values);
        provider ??= CultureInfo.InvariantCulture;

        if (typeof(T) == typeof(U8String))
        {
            return Concat(values.Cast<T, U8String>());
        }
        else if (values.TryGetSpan(out var span))
        {
            return Concat(span, format, provider);
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
            IFormatProvider provider)
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Join(byte separator, U8String[] values)
    {
        ThrowHelpers.CheckNull(values);

        return Join(separator, values.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Join(byte separator, ReadOnlySpan<U8String> values)
    {
        ThrowHelpers.CheckAscii(separator);

        return U8Manipulation.Join(separator, values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Join(byte separator, IEnumerable<U8String> values)
    {
        ThrowHelpers.CheckAscii(separator);
        ThrowHelpers.CheckNull(values);

        return U8Manipulation.Join(separator, values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            : U8Manipulation.Join(
                separator <= 0x7FF ? separator.AsTwoBytes() : separator.AsThreeBytes(),
                values);
    }

    public static U8String Join(char separator, IEnumerable<U8String> values)
    {
        ThrowHelpers.CheckSurrogate(separator);
        ThrowHelpers.CheckNull(values);

        return char.IsAscii(separator)
            ? U8Manipulation.Join((byte)separator, values)
            : U8Manipulation.Join(
                separator <= 0x7FF ? separator.AsTwoBytes() : separator.AsThreeBytes(),
                values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Join(Rune separator, U8String[] values)
    {
        ThrowHelpers.CheckNull(values);

        return Join(separator, values.AsSpan());
    }

    public static U8String Join(Rune separator, ReadOnlySpan<U8String> values)
    {
        return separator.IsAscii
            ? U8Manipulation.Join((byte)separator.Value, values)
            : U8Manipulation.Join(separator.Value switch
            {
                <= 0x7FF => separator.AsTwoBytes(),
                <= 0xFFFF => separator.AsThreeBytes(),
                _ => separator.AsFourBytes()
            }, values);
    }

    public static U8String Join(Rune separator, IEnumerable<U8String> values)
    {
        ThrowHelpers.CheckNull(values);

        return separator.IsAscii
            ? U8Manipulation.Join((byte)separator.Value, values)
            : U8Manipulation.Join(separator.Value switch
            {
                <= 0x7FF => separator.AsTwoBytes(),
                <= 0xFFFF => separator.AsThreeBytes(),
                _ => separator.AsFourBytes()
            }, values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Join(ReadOnlySpan<byte> separator, U8String[] values)
    {
        ThrowHelpers.CheckNull(values);

        return Join(separator, values.AsSpan());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Join(ReadOnlySpan<byte> separator, ReadOnlySpan<U8String> values)
    {
        Validate(separator);

        return U8Manipulation.Join(separator, values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Join(ReadOnlySpan<byte> separator, IEnumerable<U8String> values)
    {
        Validate(separator);
        ThrowHelpers.CheckNull(values);

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
            IU8Split<byte> => JoinSplitOther<byte>(separator, values),
            IU8Split<char> => JoinSplitOther<char>(separator, values),
            IU8Split<Rune> => JoinSplitOther<Rune>(separator, values),
            IU8Split<U8String> => JoinSplitOther<U8String>(separator, values),
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static U8String JoinSplitOther<S>(byte separator, T split)
            where S : struct
        {
            Debug.Assert(split is IU8Split<S>);

            // I'm sorry
            return split switch
            {
                ConfiguredU8Split<S, U8SplitOptions.TrimOptions> cst =>
                    U8Manipulation.Join<
                        ConfiguredU8Split<S, U8SplitOptions.TrimOptions>,
                        ConfiguredU8Split<S, U8SplitOptions.TrimOptions>.Enumerator>(separator, cst),
                ConfiguredU8Split<S, U8SplitOptions.RemoveEmptyOptions> csre =>
                    U8Manipulation.Join<
                        ConfiguredU8Split<S, U8SplitOptions.RemoveEmptyOptions>,
                        ConfiguredU8Split<S, U8SplitOptions.RemoveEmptyOptions>.Enumerator>(separator, csre),
                ConfiguredU8Split<S, U8SplitOptions.TrimRemoveEmptyOptions> cstre =>
                    U8Manipulation.Join<
                        ConfiguredU8Split<S, U8SplitOptions.TrimRemoveEmptyOptions>,
                        ConfiguredU8Split<S, U8SplitOptions.TrimRemoveEmptyOptions>.Enumerator>(separator, cstre),
                _ => U8Manipulation.Join(separator, split)
            };
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Join<T>(char separator, T values)
        where T : struct, IEnumerable<U8String>
    {
        ThrowHelpers.CheckSurrogate(separator);

        return char.IsAscii(separator)
            ? Join((byte)separator, values)
            : JoinSpan(
                separator <= 0x7FF ? separator.AsTwoBytes() : separator.AsThreeBytes(),
                values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Join<T>(Rune separator, T values)
        where T : struct, IEnumerable<U8String>
    {
        return separator.IsAscii
            ? Join((byte)separator.Value, values)
            : JoinSpan(separator.Value switch
            {
                <= 0x7FF => separator.AsTwoBytes(),
                <= 0xFFFF => separator.AsThreeBytes(),
                _ => separator.AsFourBytes()
            }, values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Join<T>(ReadOnlySpan<byte> separator, T values)
        where T : struct, IEnumerable<U8String>
    {
        Validate(separator);

        return separator.Length switch
        {
            0 => Concat(values),
            1 => Join(separator[0], values),
            _ => JoinSpan(separator, values)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Join<T>(U8String separator, T values)
        where T : struct, IEnumerable<U8String>
    {
        return separator.Length switch
        {
            0 => Concat(values),
            1 => Join(separator[0], values),
            _ => JoinSpan(separator, values)
        };
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
            IU8Split<byte> => JoinSplitOther<byte>(separator, values),
            IU8Split<char> => JoinSplitOther<char>(separator, values),
            IU8Split<Rune> => JoinSplitOther<Rune>(separator, values),
            IU8Split<U8String> => JoinSplitOther<U8String>(separator, values),
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static U8String JoinSplitOther<S>(ReadOnlySpan<byte> separator, T split)
            where S : struct
        {
            Debug.Assert(split is IU8Split<S>);

            return split switch
            {
                ConfiguredU8Split<S, U8SplitOptions.TrimOptions> cst =>
                    U8Manipulation.Join<
                        ConfiguredU8Split<S, U8SplitOptions.TrimOptions>,
                        ConfiguredU8Split<S, U8SplitOptions.TrimOptions>.Enumerator>(separator, cst),
                ConfiguredU8Split<S, U8SplitOptions.RemoveEmptyOptions> csre =>
                    U8Manipulation.Join<
                        ConfiguredU8Split<S, U8SplitOptions.RemoveEmptyOptions>,
                        ConfiguredU8Split<S, U8SplitOptions.RemoveEmptyOptions>.Enumerator>(separator, csre),
                ConfiguredU8Split<S, U8SplitOptions.TrimRemoveEmptyOptions> cstre =>
                    U8Manipulation.Join<
                        ConfiguredU8Split<S, U8SplitOptions.TrimRemoveEmptyOptions>,
                        ConfiguredU8Split<S, U8SplitOptions.TrimRemoveEmptyOptions>.Enumerator>(separator, cstre),
                _ => U8Manipulation.Join(separator, split)
            };
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            : JoinSpan<U8Manipulation.CharsSource>(
                separator <= 0x7FF ? separator.AsTwoBytes() : separator.AsThreeBytes(), chars.Value);
    }

    public static U8String Join(Rune separator, U8Chars chars)
    {
        return separator.IsAscii
            ? U8Manipulation.JoinRunes<U8Manipulation.CharsSource>((byte)separator.Value, chars.Value)
            : JoinSpan<U8Manipulation.CharsSource>(separator.Value switch
            {
                <= 0x7FF => separator.AsTwoBytes(),
                <= 0xFFFF => separator.AsThreeBytes(),
                _ => separator.AsFourBytes()
            }, chars.Value);
    }

    public static U8String Join(ReadOnlySpan<byte> separator, U8Chars chars)
    {
        Validate(separator);

        return JoinSpan<U8Manipulation.CharsSource>(separator, chars.Value);
    }

    public static U8String Join(U8String separator, U8Chars chars)
    {
        return JoinSpan<U8Manipulation.CharsSource>(separator, chars.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            : JoinSpan<U8Manipulation.RunesSource>(
                separator <= 0x7FF ? separator.AsTwoBytes() : separator.AsThreeBytes(), runes.Value);
    }

    public static U8String Join(Rune separator, U8Runes runes)
    {
        return separator.IsAscii
            ? U8Manipulation.JoinRunes<U8Manipulation.RunesSource>((byte)separator.Value, runes.Value)
            : JoinSpan<U8Manipulation.RunesSource>(separator.Value switch
            {
                <= 0x7FF => separator.AsTwoBytes(),
                <= 0xFFFF => separator.AsThreeBytes(),
                _ => separator.AsFourBytes()
            }, runes.Value);
    }

    public static U8String Join(ReadOnlySpan<byte> separator, U8Runes runes)
    {
        Validate(separator);

        return JoinSpan<U8Manipulation.RunesSource>(separator, runes.Value);
    }

    public static U8String Join(U8String separator, U8Runes runes)
    {
        return JoinSpan<U8Manipulation.RunesSource>(separator, runes.Value);
    }

    static U8String JoinSpan<TSource>(ReadOnlySpan<byte> separator, U8String runes)
        where TSource : struct, U8Manipulation.IRunesSource
    {
        if (separator.Length > 0)
        {
            return separator.Length is 1
                ? U8Manipulation.JoinRunes<TSource>(separator[0], runes)
                : U8Manipulation.JoinRunes<TSource>(separator, runes);
        }

        return runes;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Join<T>(
        byte separator,
        T[] values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        ThrowHelpers.CheckNull(values);

        return Join<T>(separator, values.AsSpan(), format, provider);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Join<T>(
        byte separator,
        ReadOnlySpan<T> values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        ThrowHelpers.CheckAscii(separator);

        return U8Manipulation.Join(separator, values, format, provider);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            : U8Manipulation.Join(
                separator <= 0x7FF ? separator.AsTwoBytes() : separator.AsThreeBytes(),
                values, format, provider);
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
            : U8Manipulation.Join(
                separator <= 0x7FF ? separator.AsTwoBytes() : separator.AsThreeBytes(),
                values, format, provider);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            : U8Manipulation.Join(separator.Value switch
            {
                <= 0x7FF => separator.AsTwoBytes(),
                <= 0xFFFF => separator.AsThreeBytes(),
                _ => separator.AsFourBytes()
            }, values, format, provider);
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
            : U8Manipulation.Join(separator.Value switch
            {
                <= 0x7FF => separator.AsTwoBytes(),
                <= 0xFFFF => separator.AsThreeBytes(),
                _ => separator.AsFourBytes()
            }, values, format, provider);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    /// <exception cref="FormatException">
    /// The resulting <see cref="U8String"/> is not a valid UTF-8 byte sequence.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String Remove(byte value) => U8Manipulation.Remove(this, value);

    /// <inheritdoc cref="Remove(U8String)"/>
    /// <exception cref="ArgumentException">
    /// The <paramref name="value"/> is a surrogate.
    /// </exception>
    public U8String Remove(char value)
    {
        ThrowHelpers.CheckSurrogate(value);

        return char.IsAscii(value)
            ? U8Manipulation.Remove(this, (byte)value)
            : U8Manipulation.Remove(
                this, value <= 0x7FF ? value.AsTwoBytes() : value.AsThreeBytes(), validate: false);
    }

    /// <inheritdoc cref="Remove(U8String)"/>
    public U8String Remove(Rune value) => value.IsAscii
        ? U8Manipulation.Remove(this, (byte)value.Value)
        : U8Manipulation.Remove(this, value.Value switch
        {
            <= 0x7FF => value.AsTwoBytes(),
            <= 0xFFFF => value.AsThreeBytes(),
            _ => value.AsFourBytes()
        }, validate: false);

    /// <inheritdoc cref="Remove(U8String)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String Remove(ReadOnlySpan<byte> value) => value.Length is 1
        ? U8Manipulation.Remove(this, value[0])
        : U8Manipulation.Remove(this, value);

    /// <summary>
    /// Removes all occurrences of <paramref name="value"/> from the current <see cref="U8String"/>.
    /// </summary>
    /// <param name="value">The element to remove from the current <see cref="U8String"/>.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    public U8String ReplaceLineEndings(char lineEnding)
    {
        ThrowHelpers.CheckSurrogate(lineEnding);

        return char.IsAscii(lineEnding)
            ? ReplaceLineEndings((byte)lineEnding)
            : U8Manipulation.LineEndingsToCustom(
                this, lineEnding <= 0x7FF ? lineEnding.AsTwoBytes() : lineEnding.AsThreeBytes());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String ReplaceLineEndings(Rune lineEnding)
    {
        return lineEnding.IsAscii
            ? ReplaceLineEndings((byte)lineEnding.Value)
            : U8Manipulation.LineEndingsToCustom(this, lineEnding.Value switch
            {
                <= 0x7FF => lineEnding.AsTwoBytes(),
                <= 0xFFFF => lineEnding.AsThreeBytes(),
                _ => lineEnding.AsFourBytes()
            });
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
        var offset = start > 0 ? Math.Min(start, source.Length) : 0;
        var newlength = source.Length - offset;
        offset += source.Offset;

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
        var (source, sourceOffset, sourceLength) = this;

        var newStart = start > 0 ? Math.Min(start, sourceLength) : 0;
        var newLength = length > 0 ? Math.Min(length, sourceLength - newStart) : 0;

        if (newLength > 0)
        {
            ref var ptr = ref source!.AsRef(sourceOffset);
            if (newStart > 0)
            {
                while (newLength > 0
                    && U8Info.IsContinuationByte(in ptr.Add(newStart)))
                {
                    newStart++;
                    newLength--;
                }
            }

            if (newLength < sourceLength - newStart)
            {
                ptr = ref ptr.Add(newStart);
                while (newLength > 0
                    && U8Info.IsContinuationByte(in ptr.Add(newLength)))
                {
                    newLength--;
                }
            }
        }

        return new(source, sourceOffset + newStart, newLength);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String Strip(byte value)
    {
        ThrowHelpers.CheckAscii(value);

        var (source, offset, length) = this;
        if (source != null)
        {
            ref var ptr = ref source.AsRef(offset);

            if (ptr.Add(length - 1) == value)
            {
                length--;
            }

            if (length > 0 && ptr == value)
            {
                offset++;
                length--;
            }
        }

        return new(source, offset, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String Strip(char value)
    {
        ThrowHelpers.CheckSurrogate(value);

        return value <= 0x7F
            ? Strip((byte)value)
            : NonAscii(value, this);

        static U8String NonAscii(char value, U8String source)
        {
            return source.StripUnchecked(value <= 0x7FF ? value.AsTwoBytes() : value.AsThreeBytes());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String Strip(Rune value)
    {
        return value.Value <= 0x7F
            ? Strip((byte)value.Value)
            : NonAscii(value, this);

        static U8String NonAscii(Rune value, U8String source)
        {
            return source.StripUnchecked(value.Value switch
            {
                <= 0x7FF => value.AsTwoBytes(),
                <= 0xFFFF => value.AsThreeBytes(),
                _ => value.AsFourBytes()
            });
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String Strip(ReadOnlySpan<byte> value)
    {
        // TODO: Author exception type, introduce ValidateArgument/ValidateThrowArgumentException?
        Validate(value);

        return StripUnchecked(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String Strip(U8String value)
    {
        return !value.IsEmpty ? StripUnchecked(value) : this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8String StripUnchecked(ReadOnlySpan<byte> value)
    {
        var (source, offset, length) = this;
        if (source != null)
        {
            ref var ptr = ref source.AsRef(offset);

            if (length >= value.Length)
            {
                if (ptr.AsSpan(length).EndsWith(value))
                {
                    length -= value.Length;
                }

                if (length >= value.Length && ptr.AsSpan(length).StartsWith(value))
                {
                    offset += value.Length;
                    length -= value.Length;
                }
            }
        }

        return new(source, offset, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String Strip(byte prefix, byte suffix)
    {
        ThrowHelpers.CheckAscii(prefix);
        ThrowHelpers.CheckAscii(suffix);

        var (source, offset, length) = this;
        if (source != null)
        {
            ref var ptr = ref source.AsRef(offset);

            if (ptr.Add(length - 1) == suffix)
            {
                length--;
            }

            if (length > 0 && ptr == prefix)
            {
                offset++;
                length--;
            }
        }

        return new(source, offset, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String Strip(char prefix, char suffix)
    {
        ThrowHelpers.CheckSurrogate(prefix);
        ThrowHelpers.CheckSurrogate(suffix);

        return prefix <= 0x7F && suffix <= 0x7F
            ? Strip((byte)prefix, (byte)suffix)
            : NonAscii(prefix, suffix, this);

        static U8String NonAscii(char prefix, char suffix, U8String source)
        {
            ReadOnlySpan<byte> prefixValue = (ushort)prefix switch
            {
                <= 0x7F => [(byte)prefix],
                <= 0x7FF => prefix.AsTwoBytes(),
                _ => prefix.AsThreeBytes()
            };

            ReadOnlySpan<byte> suffixValue = (ushort)suffix switch
            {
                <= 0x7F => [(byte)suffix],
                <= 0x7FF => suffix.AsTwoBytes(),
                _ => suffix.AsThreeBytes()
            };

            return source.StripUnchecked(prefixValue, suffixValue);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String Strip(Rune prefix, Rune suffix)
    {
        return prefix.Value <= 0x7F && suffix.Value <= 0x7F
            ? Strip((byte)prefix.Value, (byte)suffix.Value)
            : NonAscii(prefix, suffix, this);

        static U8String NonAscii(Rune prefix, Rune suffix, U8String source)
        {
            ReadOnlySpan<byte> prefixValue = prefix.Value switch
            {
                <= 0x7F => [(byte)prefix.Value],
                <= 0x7FF => prefix.AsTwoBytes(),
                <= 0xFFFF => prefix.AsThreeBytes(),
                _ => prefix.AsFourBytes()
            };

            ReadOnlySpan<byte> suffixValue = suffix.Value switch
            {
                <= 0x7F => [(byte)suffix.Value],
                <= 0x7FF => suffix.AsTwoBytes(),
                <= 0xFFFF => suffix.AsThreeBytes(),
                _ => suffix.AsFourBytes()
            };

            return source.StripUnchecked(prefixValue, suffixValue);
        }
    }

    public U8String Strip(ReadOnlySpan<byte> prefix, ReadOnlySpan<byte> suffix)
    {
        Validate(prefix);
        Validate(suffix);

        return StripUnchecked(prefix, suffix);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String Strip(U8String prefix, U8String suffix)
    {
        return StripUnchecked(prefix, suffix);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8String StripUnchecked(ReadOnlySpan<byte> prefix, ReadOnlySpan<byte> suffix)
    {
        var (source, offset, length) = this;
        if (source != null)
        {
            ref var ptr = ref source.AsRef(offset);

            if (length >= suffix.Length && ptr.AsSpan(length).EndsWith(suffix))
            {
                length -= suffix.Length;
            }

            if (length >= prefix.Length && ptr.AsSpan(length).StartsWith(prefix))
            {
                offset += prefix.Length;
                length -= prefix.Length;
            }
        }

        return new(source, offset, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String StripPrefix(byte prefix)
    {
        ThrowHelpers.CheckAscii(prefix);

        var (source, offset, length) = this;
        if (source != null &&
            source.AsRef(offset) == prefix)
        {
            offset++;
            length--;
        }

        return new(source, offset, length);
    }

    public U8String StripPrefix(char prefix)
    {
        ThrowHelpers.CheckSurrogate(prefix);

        return char.IsAscii(prefix)
            ? StripPrefix((byte)prefix)
            : StripPrefixUnchecked(prefix <= 0x7FF ? prefix.AsTwoBytes() : prefix.AsThreeBytes());
    }

    public U8String StripPrefix(Rune prefix)
    {
        return prefix.IsAscii
            ? StripPrefix((byte)prefix.Value)
            : StripPrefixUnchecked(prefix.Value switch
            {
                <= 0x7FF => prefix.AsTwoBytes(),
                <= 0xFFFF => prefix.AsThreeBytes(),
                _ => prefix.AsFourBytes()
            });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String StripPrefix(ReadOnlySpan<byte> prefix)
    {
        // TODO: Another callside to replace with plain Validate
        // once FoldValidations opt. pass is implemented in U8String.Optimization
        Validate(prefix);

        return StripPrefixUnchecked(prefix);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String StripPrefix(U8String prefix)
    {
        return StripPrefixUnchecked(prefix);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8String StripPrefixUnchecked(ReadOnlySpan<byte> prefix)
    {
        var (source, offset, length) = this;
        if (source != null
            && prefix.Length <= length
            && source.SliceUnsafe(offset, length).StartsWith(prefix))
        {
            offset += prefix.Length;
            length -= prefix.Length;
        }

        return new(source, offset, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String StripSuffix(byte suffix)
    {
        ThrowHelpers.CheckAscii(suffix);

        var (source, offset, length) = this;
        if (source != null &&
            source.AsRef(offset + length - 1) == suffix)
        {
            length--;
        }

        return new(source, offset, length);
    }

    public U8String StripSuffix(char suffix)
    {
        ThrowHelpers.CheckSurrogate(suffix);

        // TODO: Apply the same IL size optimizations around inlining
        // as in Strip(prefix, suffix)
        return char.IsAscii(suffix)
            ? StripSuffix((byte)suffix)
            : StripSuffixUnchecked(suffix <= 0x7FF ? suffix.AsTwoBytes() : suffix.AsThreeBytes());
    }

    public U8String StripSuffix(Rune suffix)
    {
        return suffix.IsAscii
            ? StripSuffix((byte)suffix.Value)
            : StripSuffixUnchecked(suffix.Value switch
            {
                <= 0x7FF => suffix.AsTwoBytes(),
                <= 0xFFFF => suffix.AsThreeBytes(),
                _ => suffix.AsFourBytes()
            });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String StripSuffix(ReadOnlySpan<byte> suffix)
    {
        Validate(suffix);

        return StripSuffixUnchecked(suffix);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String StripSuffix(U8String suffix)
    {
        return StripSuffixUnchecked(suffix);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8String StripSuffixUnchecked(ReadOnlySpan<byte> suffix)
    {
        var (source, offset, length) = this;
        if (source != null
            && suffix.Length <= length
            && source.SliceUnsafe(offset, length).EndsWith(suffix))
        {
            length -= suffix.Length;
        }

        return new(source, offset, length);
    }

    /// <summary>
    /// Removes all leading and trailing whitespace characters from the current string.
    /// </summary>
    /// <remarks>
    /// 'Whitespace characters' refers to all runes for which <see cref="Rune.IsWhiteSpace"/>
    /// returns <see langword="true"/>.
    /// </remarks>
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
                goto Likely;
            }

            return TrimCore(source._value, source.Offset, source.Length);
        }

        Likely:
        return source;

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
                        // for more whitespace byte sequences from their end. If we don't do this,
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
    /// <remarks>
    /// 'Whitespace characters' refers to all runes for which <see cref="Rune.IsWhiteSpace"/>
    /// returns <see langword="true"/>.
    /// </remarks>
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
    /// <remarks>
    /// 'Whitespace characters' refers to all runes for which <see cref="Rune.IsWhiteSpace"/>
    /// returns <see langword="true"/>.
    /// </remarks>
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
    /// <remarks>
    /// 'Whitespace characters' refers to all bytes for which <see cref="U8Info.IsAsciiWhitespace"/>
    /// returns <see langword="true"/>.
    /// </remarks>
    /// <returns>
    /// A sub-slice that remains after all ASCII whitespace characters
    /// are removed from the start and end of the current string.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String TrimAscii()
    {
        var source = this;

        return !source.IsEmpty
            ? U8Marshal.SliceUnsafe(source, Ascii.Trim(source.UnsafeSpan))
            : source;
    }

    /// <summary>
    /// Removes all the leading ASCII whitespace characters from the current string.
    /// </summary>
    /// <remarks>
    /// 'Whitespace characters' refers to all bytes for which <see cref="U8Info.IsAsciiWhitespace"/>
    /// returns <see langword="true"/>.
    /// </remarks>
    /// <returns>
    /// A sub-slice that remains after all whitespace characters
    /// are removed from the start of the current string.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String TrimStartAscii()
    {
        var source = this;

        return !source.IsEmpty
            ? U8Marshal.SliceUnsafe(source, Ascii.TrimStart(source.UnsafeSpan))
            : source;
    }

    /// <summary>
    /// Removes all the trailing ASCII whitespace characters from the current string.
    /// </summary>
    /// <remarks>
    /// 'Whitespace characters' refers to all bytes for which <see cref="U8Info.IsAsciiWhitespace"/>
    /// returns <see langword="true"/>.
    /// </remarks>
    /// <returns>
    /// A sub-slice that remains after all whitespace characters
    /// are removed from the end of the current string.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String TrimEndAscii()
    {
        var source = this;

        return !source.IsEmpty
            ? U8Marshal.SliceUnsafe(source, Ascii.TrimEnd(source.UnsafeSpan))
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
        var deref = this;
        if (!deref.IsEmpty)
        {
            var source = deref.UnsafeSpan;
            var replaceStart = converter.FindToLowerStart(source);
            if (replaceStart >= 0 && replaceStart < source.Length)
            {
                return converter.IsFixedLength
                    ? ToLowerFixed(source, replaceStart, converter)
                    : ToLowerVariable(source, replaceStart, converter);
            }
        }

        return deref;

        static U8String ToLowerFixed(ReadOnlySpan<byte> source, int start, T converter)
        {
            Debug.Assert(start >= 0);
            Debug.Assert(start < source.Length);
            Debug.Assert(converter.IsFixedLength);

            var nullTerminate = source[^1] != 0;
            var result = new byte[source.Length + (nullTerminate ? 1 : 0)];
            var destination = result.AsSpan(start);

            source.Slice(0, start).CopyToUnsafe(ref result.AsRef());
            converter.ToLower(source.SliceUnsafe(start), destination);

            return new(result, source.Length, neverEmpty: true);
        }

        static U8String ToLowerVariable(ReadOnlySpan<byte> source, int start, T converter)
        {
            Debug.Assert(start >= 0);
            Debug.Assert(start < source.Length);
            Debug.Assert(!converter.IsFixedLength);

            var destination = new InlineU8Builder(source.Length);

            destination.AppendBytes(source.Slice(0, start));
            converter.ToLower(source.SliceUnsafe(start), ref destination);

            var result = new U8String(destination.Written, skipValidation: true);
            destination.Dispose();
            return result;
        }
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
            var source = deref.UnsafeSpan;
            var replaceStart = converter.FindToUpperStart(source);
            if (replaceStart >= 0 && replaceStart < source.Length)
            {
                return converter.IsFixedLength
                    ? ToUpperFixed(source, replaceStart, converter)
                    : ToUpperVariable(source, replaceStart, converter);
            }
        }

        return deref;

        static U8String ToUpperFixed(ReadOnlySpan<byte> source, int start, T converter)
        {
            Debug.Assert(start >= 0);
            Debug.Assert(start < source.Length);
            Debug.Assert(converter.IsFixedLength);

            var nullTerminate = source[^1] != 0;
            var result = new byte[source.Length + (nullTerminate ? 1 : 0)];
            var destination = result.AsSpan(start);

            source.Slice(0, start).CopyToUnsafe(ref result.AsRef());
            converter.ToUpper(source.SliceUnsafe(start), destination);

            return new(result, source.Length, neverEmpty: true);
        }

        static U8String ToUpperVariable(ReadOnlySpan<byte> source, int start, T converter)
        {
            Debug.Assert(start >= 0);
            Debug.Assert(start < source.Length);
            Debug.Assert(!converter.IsFixedLength);

            var destination = new InlineU8Builder(source.Length);

            destination.AppendBytes(source.Slice(0, start));
            converter.ToUpper(source.SliceUnsafe(start), ref destination);

            var result = new U8String(destination.Written, skipValidation: true);
            destination.Dispose();
            return result;
        }
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
