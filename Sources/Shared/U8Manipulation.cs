using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace U8Primitives;

internal static class U8Manipulation
{
    internal static U8String ConcatUnchecked(ReadOnlySpan<byte> left, byte right)
    {
        Debug.Assert(U8Info.IsAsciiByte(right));

        var length = left.Length + 1;
        var value = new byte[length + 1];

        ref var dst = ref value.AsRef();
        left.CopyToUnsafe(ref dst);
        dst.Add(left.Length) = right;

        return new U8String(value, 0, length);
    }

    internal static U8String ConcatUnchecked(byte left, ReadOnlySpan<byte> right)
    {
        Debug.Assert(U8Info.IsAsciiByte(left));

        var length = right.Length + 1;
        var value = new byte[length + 1];

        ref var dst = ref value.AsRef();
        dst = left;
        right.CopyToUnsafe(ref dst.Add(1));

        return new U8String(value, 0, length);
    }

    internal static U8String ConcatUnchecked(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        Debug.Assert(!left.IsEmpty || !right.IsEmpty);

        var length = left.Length + right.Length;
        var value = new byte[length + 1];

        ref var dst = ref value.AsRef();
        left.CopyToUnsafe(ref dst);
        right.CopyToUnsafe(ref dst.Add(left.Length));

        return new U8String(value, 0, length);
    }

    internal static U8String Join(byte separator, ReadOnlySpan<U8String> values)
    {
        if (values.Length > 1)
        {
            return JoinUnchecked(separator, values);
        }
        else if (values.Length is 1)
        {
            return values[0];
        }

        return default;
    }

    internal static U8String Join(byte separator, IEnumerable<U8String> values)
    {
        if (values is U8String[] array)
        {
            return Join(separator, array.AsSpan());
        }
        else if (values is List<U8String> list)
        {
            return Join(separator, CollectionsMarshal.AsSpan(list));
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

        return JoinEnumerable(separator, values);

        static U8String JoinEnumerable(byte separator, IEnumerable<U8String> values)
        {
            using var enumerator = values.GetEnumerator();
            var builder = new ArrayBuilder();

            if (enumerator.MoveNext())
            {
                builder.Write(enumerator.Current.AsSpan());

                while (enumerator.MoveNext())
                {
                    builder.Write(separator);

                    var current = enumerator.Current;
                    if (!current.IsEmpty)
                    {
                        builder.Write(current.UnsafeSpan);
                    }
                }
            }

            var result = new U8String(builder.Written, skipValidation: true);

            builder.Dispose();
            return result;
        }
    }

    internal static U8String Join(ReadOnlySpan<byte> separator, ReadOnlySpan<U8String> values)
    {
        if (values.Length > 1)
        {
            if (separator.Length > 1)
            {
                return JoinUnchecked(separator, values);
            }
            else if (separator.Length is 1)
            {
                return JoinUnchecked(separator[0], values);
            }

            return U8String.Concat(values);
        }
        else if (values.Length is 1)
        {
            return values[0];
        }

        return default;
    }

    internal static U8String Join(ReadOnlySpan<byte> separator, IEnumerable<U8String> values)
    {
        if (values is U8String[] array)
        {
            return Join(separator, array.AsSpan());
        }
        else if (values is List<U8String> list)
        {
            return Join(separator, CollectionsMarshal.AsSpan(list));
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

        return JoinEnumerable(separator, values);

        static U8String JoinEnumerable(ReadOnlySpan<byte> separator, IEnumerable<U8String> values)
        {
            using var enumerator = values.GetEnumerator();
            var builder = new ArrayBuilder();

            if (enumerator.MoveNext())
            {
                builder.Write(enumerator.Current.AsSpan());

                while (enumerator.MoveNext())
                {
                    builder.Write(separator);

                    var current = enumerator.Current;
                    if (!current.IsEmpty)
                    {
                        builder.Write(current.UnsafeSpan);
                    }
                }
            }

            var result = new U8String(builder.Written, skipValidation: true);

            builder.Dispose();
            return result;
        }
    }

    internal static U8String Join<T>(
        byte separator,
        ReadOnlySpan<T> values,
        ReadOnlySpan<char> format,
        IFormatProvider? provider) where T : IUtf8SpanFormattable
    {
        if (values.Length > 1)
        {
            return JoinUnchecked(separator, values, format, provider);
        }
        else if (values.Length is 1)
        {
            return U8String.Create(values[0], format, provider);
        }

        return default;
    }

    internal static U8String Join<T>(
        byte separator,
        IEnumerable<T> values,
        ReadOnlySpan<char> format,
        IFormatProvider? provider) where T : IUtf8SpanFormattable
    {
        if (values is T[] array)
        {
            return Join<T>(separator, array.AsSpan(), format, provider);
        }
        else if (values is List<T> list)
        {
            return Join<T>(separator, CollectionsMarshal.AsSpan(list), format, provider);
        }
        else if (values.TryGetNonEnumeratedCount(out var count))
        {
            if (count is 1)
            {
                return U8String.Create(values.First(), format, provider);
            }
            else if (count is 0)
            {
                return default;
            }
        }

        return JoinUnchecked(separator, values, format, provider);
    }

    internal static U8String Join<T>(
        ReadOnlySpan<byte> separator,
        ReadOnlySpan<T> values,
        ReadOnlySpan<char> format,
        IFormatProvider? provider) where T : IUtf8SpanFormattable
    {
        if (values.Length > 1)
        {
            if (separator.Length > 1)
            {
                return JoinUnchecked(separator, values, format, provider);
            }
            else if (separator.Length is 1)
            {
                return JoinUnchecked(separator[0], values, format, provider);
            }

            return U8String.Concat(values, format, provider);
        }
        else if (values.Length is 1)
        {
            return U8String.Create(values[0], format, provider);
        }

        return default;
    }

    internal static U8String Join<T>(
        ReadOnlySpan<byte> separator,
        IEnumerable<T> values,
        ReadOnlySpan<char> format,
        IFormatProvider? provider) where T : IUtf8SpanFormattable
    {
        if (values is T[] array)
        {
            return Join<T>(separator, array.AsSpan(), format, provider);
        }
        else if (values is List<T> list)
        {
            return Join<T>(separator, CollectionsMarshal.AsSpan(list), format, provider);
        }
        else if (values.TryGetNonEnumeratedCount(out var count))
        {
            if (count is 1)
            {
                return U8String.Create(values.First(), format, provider);
            }
            else if (count is 0)
            {
                return default;
            }
        }

        return JoinUnchecked(separator, values, format, provider);
    }

    // Contract:
    // - values.Length is greater than 1
    // - separator is a non-ascii byte, char, Rune or U8Scalar
    internal static U8String JoinUnchecked(byte separator, ReadOnlySpan<U8String> value)
    {
        Debug.Assert(value.Length > 1);

        var length = value.Length - 1;
        foreach (var item in value)
        {
            length += item.Length;
        }

        var result = new byte[length + 1]; // null-terminate
        ref var dst = ref result.AsRef();

        var first = value.AsRef();
        first.AsSpan().CopyToUnsafe(ref dst);
        dst = ref dst.Add(first.Length);

        var offset = 1;
        ref var ptr = ref value.AsRef();

        // foreach emits rngcheck so we have to do this manually.
        while (offset < value.Length)
        {
            dst = separator;
            dst = ref dst.Add(1);

            var item = ptr.Add(offset);
            item.AsSpan().CopyToUnsafe(ref dst);
            dst = ref dst.Add(item.Length);

            offset++;
        }

        return new(result, 0, length);
    }

    // Contract:
    // - values.Length is greater than 1
    // - separator is a non-ascii byte, char, Rune or U8Scalar
    internal static U8String JoinUnchecked(ReadOnlySpan<byte> separator, ReadOnlySpan<U8String> values)
    {
        Debug.Assert(values.Length > 1);
        Debug.Assert(separator.Length > 1);

        var length = separator.Length * (values.Length - 1);
        foreach (var value in values)
        {
            length += value.Length;
        }

        var result = new byte[length + 1]; // null-terminate
        ref var dst = ref result.AsRef();

        var first = values.AsRef();
        first.AsSpan().CopyToUnsafe(ref dst);
        dst = ref dst.Add(first.Length);
        values = values.SliceUnsafe(1);

        switch (separator.Length)
        {
            case 2:
                var twoBytes = Unsafe.As<byte, ushort>(ref separator.AsRef());
                JoinTwoBytes(twoBytes, values, ref dst);
                break;
            case 3:
                var threeBytes = (uint)(
                    Unsafe.As<byte, ushort>(ref separator.AsRef()) | (separator[2] << 16));
                JoinThreeBytes(threeBytes, values, ref dst);
                break;
            case 4:
                var fourBytes = Unsafe.As<byte, uint>(ref separator.AsRef());
                JoinFourBytes(fourBytes, values, ref dst);
                break;
            default:
                JoinSpan(separator, values, ref dst);
                break;
        }

        return new(result, 0, length);

        static void JoinTwoBytes(ushort separator, ReadOnlySpan<U8String> values, ref byte dst)
        {
            foreach (var value in values)
            {
                Unsafe.As<byte, ushort>(ref dst) = separator;
                dst = ref dst.Add(2);

                value.AsSpan().CopyToUnsafe(ref dst);
                dst = ref dst.Add(value.Length);
            }
        }

        static void JoinThreeBytes(uint separator, ReadOnlySpan<U8String> values, ref byte dst)
        {
            foreach (var value in values)
            {
                Unsafe.As<byte, uint>(ref dst) = separator;
                dst = ref dst.Add(3);

                value.AsSpan().CopyToUnsafe(ref dst);
                dst = ref dst.Add(value.Length);
            }
        }

        static void JoinFourBytes(uint separator, ReadOnlySpan<U8String> values, ref byte dst)
        {
            foreach (var value in values)
            {
                Unsafe.As<byte, uint>(ref dst) = separator;
                dst = ref dst.Add(4);

                value.AsSpan().CopyToUnsafe(ref dst);
                dst = ref dst.Add(value.Length);
            }
        }

        static void JoinSpan(ReadOnlySpan<byte> separator, ReadOnlySpan<U8String> values, ref byte dst)
        {
            foreach (var value in values)
            {
                separator.CopyToUnsafe(ref dst);
                dst = ref dst.Add(separator.Length);

                value.AsSpan().CopyToUnsafe(ref dst);
                dst = ref dst.Add(value.Length);
            }
        }
    }

    internal static U8String JoinUnchecked<T>(
        byte separator,
        ReadOnlySpan<T> values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        Debug.Assert(values.Length > 1);

        var builder = new ArrayBuilder();
        var first = MemoryMarshal.GetReference(values);
        builder.Write(first, format, provider);

        foreach (var value in values[1..])
        {
            builder.Write(separator);
            builder.Write(value, format, provider);
        }

        var result = new U8String(builder.Written, skipValidation: true);

        builder.Dispose();
        return result;
    }

    internal static U8String JoinUnchecked<T>(
        ReadOnlySpan<byte> separator,
        ReadOnlySpan<T> values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        Debug.Assert(separator.Length > 1);
        Debug.Assert(values.Length > 1);

        var builder = new ArrayBuilder();
        var first = MemoryMarshal.GetReference(values);
        builder.Write(first, format, provider);

        foreach (var value in values[1..])
        {
            builder.Write(separator);
            builder.Write(value, format, provider);
        }

        var result = new U8String(builder.Written, skipValidation: true);

        builder.Dispose();
        return result;
    }

    internal static U8String JoinUnchecked<T>(
        byte separator,
        IEnumerable<T> values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        Debug.Assert(values is not (T[] or List<T>));

        using var enumerator = values.GetEnumerator();
        var builder = new ArrayBuilder();

        if (enumerator.MoveNext())
        {
            builder.Write(enumerator.Current, format, provider);

            while (enumerator.MoveNext())
            {
                builder.Write(separator);
                builder.Write(enumerator.Current, format, provider);
            }
        }

        var result = new U8String(builder.Written, skipValidation: true);

        builder.Dispose();
        return result;
    }

    internal static U8String JoinUnchecked<T>(
        ReadOnlySpan<byte> separator,
        IEnumerable<T> values,
        ReadOnlySpan<char> format = default,
        IFormatProvider? provider = null) where T : IUtf8SpanFormattable
    {
        Debug.Assert(separator.Length > 1);
        Debug.Assert(values is not (T[] or List<T>));

        using var enumerator = values.GetEnumerator();
        var builder = new ArrayBuilder();

        if (enumerator.MoveNext())
        {
            builder.Write(enumerator.Current, format, provider);

            while (enumerator.MoveNext())
            {
                builder.Write(separator);
                builder.Write(enumerator.Current, format, provider);
            }
        }

        var result = new U8String(builder.Written, skipValidation: true);

        builder.Dispose();
        return result;
    }

    internal static U8String Replace(
        U8String source,
        byte oldValue,
        byte newValue,
        bool validate = true)
    {
        if (!source.IsEmpty)
        {
            var current = source.UnsafeSpan;
            var firstReplace = current.IndexOf(oldValue);
            if (firstReplace >= 0)
            {
                var replaced = new byte[source.Length + 1];
                var destination = replaced.AsSpan();

                current
                    .SliceUnsafe(0, firstReplace)
                    .CopyTo(destination.SliceUnsafe(0, firstReplace));

                destination = destination.SliceUnsafe(firstReplace);
                current = current.SliceUnsafe(firstReplace);

                current.Replace(
                    destination.SliceUnsafe(0, current.Length),
                    oldValue,
                    newValue);

                // Old and new bytes which individually are invalid unicode scalar values
                // are allowed if the replacement produces a valid UTF-8 sequence.
                if (validate && (
                    !U8Info.IsAsciiByte(oldValue) ||
                    !U8Info.IsAsciiByte(newValue)))
                {
                    U8String.Validate(destination);
                }
                return new(replaced, 0, source.Length);
            }

            return source;
        }

        return default;
    }

    internal static U8String Replace(U8String source, char oldValue, char newValue)
    {
        ThrowHelpers.CheckSurrogate(oldValue);

        return char.IsAscii(oldValue) && char.IsAscii(newValue)
            ? Replace(source, (byte)oldValue, (byte)newValue, validate: false)
            : ReplaceCore(
                source,
                new U8Scalar(oldValue).AsSpan(),
                new U8Scalar(newValue).AsSpan());
    }

    internal static U8String Replace(U8String source, Rune oldValue, Rune newValue)
    {
        return oldValue.IsAscii && newValue.IsAscii
            ? Replace(source, (byte)oldValue.Value, (byte)newValue.Value, validate: false)
            : ReplaceCore(
                source,
                new U8Scalar(oldValue).AsSpan(),
                new U8Scalar(newValue).AsSpan());
    }

    internal static U8String Replace(U8String source, ReadOnlySpan<byte> oldValue, ReadOnlySpan<byte> newValue)
    {
        if (oldValue.Length is 0)
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        if (!source.IsEmpty)
        {
            if (newValue.Length is 0)
            {
                return oldValue.Length is 1
                    ? Remove(source, oldValue[0])
                    : Remove(source, oldValue);
            }

            if (oldValue.Length is 1 && newValue.Length is 1)
            {
                return Replace(source, oldValue[0], newValue[0]);
            }

            return ReplaceCore(source, oldValue, newValue);
        }

        return default;
    }

    internal static U8String Replace(U8String source, U8String oldValue, U8String newValue)
    {
        if (oldValue.IsEmpty)
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        if (!source.IsEmpty)
        {
            return ReplaceCore(
                source,
                oldValue.UnsafeSpan,
                newValue.AsSpan(),
                validate: false);
        }

        return default;
    }

    /// <summary>
    /// Assumes the following checks are performed outside of this method:
    /// <para>- <paramref name="source"/> is not empty</para>
    /// <para>- <paramref name="oldValue"/> is not empty</para>
    /// <para>- <paramref name="newValue"/> is not empty</para>
    /// <para>- <paramref name="oldValue"/> and <paramref name="newValue"/> lengths are greater than 1</para>
    /// </summary>
    internal static U8String ReplaceCore(
        U8String source,
        ReadOnlySpan<byte> oldValue,
        ReadOnlySpan<byte> newValue,
        bool validate = true)
    {
        Debug.Assert(!oldValue.IsEmpty);
        Debug.Assert(!newValue.IsEmpty);
        Debug.Assert(oldValue.Length != 0 && newValue.Length != 0);
        Debug.Assert(oldValue.Length >= 1 || newValue.Length >= 1);

        var count = source.AsSpan().Count(oldValue);
        if (count > 0)
        {
            var length = source.Length - (oldValue.Length * count) + (newValue.Length * count);

            var result = new byte[length + 1];

            var offset = 0;
            ref var dst = ref result.AsRef();
            foreach (var segment in new U8RefSplit(source, oldValue))
            {
                segment.AsSpan().CopyToUnsafe(ref dst.Add(offset));

                if ((offset += segment.Length) >= length)
                {
                    break;
                }

                newValue.CopyToUnsafe(ref dst.Add(offset));
                offset += newValue.Length;
            }

            if (validate)
            {
                U8String.Validate(result);
            }

            return new(result, 0, result.Length);
        }

        return source;
    }

    internal static U8String Remove(U8String source, byte value)
    {
        var count = source.AsSpan().Count(value);
        if (count > 0)
        {
            var length = source.Length - count;
            var result = new byte[length + 1];

            var offset = 0;
            ref var dst = ref result.AsRef();
            foreach (var segment in new U8Split<byte>(source, value))
            {
                segment.AsSpan().CopyToUnsafe(ref dst.Add(offset));
                offset += segment.Length;
            }

            if (!U8Info.IsAsciiByte(value))
            {
                U8String.Validate(result);
            }

            return new(result, 0, result.Length);
        }

        return source;
    }

    internal static U8String Remove(U8String source, ReadOnlySpan<byte> value, bool validate = true)
    {
        var count = source.AsSpan().Count(value);
        if (count > 0)
        {
            var length = source.Length - (value.Length * count);
            var result = new byte[length + 1];

            var offset = 0;
            ref var dst = ref result.AsRef();
            foreach (var segment in new U8RefSplit(source, value))
            {
                segment.AsSpan().CopyToUnsafe(ref dst.Add(offset));
                offset += segment.Length;
            }

            if (validate)
            {
                U8String.Validate(result);
            }
            return new(result, 0, result.Length);
        }

        return source;
    }

    internal static U8String StripLineEndings(U8String source)
    {
        var lines = source.Lines;
        if (lines.Count > 1)
        {
            var builder = new ArrayBuilder();
            foreach (var line in lines)
            {
                if (!line.IsEmpty)
                {
                    builder.Write(line.UnsafeSpan);
                }
            }

            var result = new U8String(builder.Written, skipValidation: true);

            builder.Dispose();
            return result;
        }

        return source;
    }

    internal static U8String LineEndingsToLF(U8String source)
    {
        return ReplaceCore(source, "\r\n"u8, "\n"u8, validate: false);
    }

    internal static U8String LineEndingsToCRLF(U8String source)
    {
        if (!source.IsEmpty)
        {
            // This method operates on absolute offsets
            var array = source._value!;
            var range = source._inner;

            while (true)
            {
                var span = array.SliceUnsafe(range);
                var offset = span.IndexOf((byte)'\n');

                if ((uint)offset < (uint)span.Length)
                {
                    range = new(
                        range.Offset + offset + 1,
                        range.Length - offset - 1);

                    if (offset > 0 && span[offset - 1] is (byte)'\r')
                    {
                        continue;
                    }
                    // Range is now the slice after the first LF -> CRLF replacement
                    else goto Replace;
                }

                return source;
            }

        Replace:
            var firstReplace = range.Offset - 1;
            var builder = new ArrayBuilder();

            // Copy the first part of the string before the first LF -> CRLF
            builder.Write(array.SliceUnsafe(
                source._inner.Offset, firstReplace - source._inner.Offset));
            builder.Write("\r\n"u8);

            foreach (var line in new U8String(array, range).Lines)
            {
                if (!line.IsEmpty)
                    builder.Write(line.UnsafeSpan);
                builder.Write("\r\n"u8);
            }

            var result = new U8String(builder.Written, skipValidation: true);

            builder.Dispose();
            return result;
        }

        return default;
    }

    internal static U8String LineEndingsToCustom(U8String source, ReadOnlySpan<byte> lineEnding)
    {
        Debug.Assert(lineEnding.Length > 0);

        var lines = source.Lines;
        if (lines.Count > 1)
        {
            var builder = new ArrayBuilder();
            var enumerator = lines.GetEnumerator();

            enumerator.MoveNext();
            var line = enumerator.Current;
            if (!line.IsEmpty)
            {
                builder.Write(line.UnsafeSpan);
            }

            while (enumerator.MoveNext())
            {
                builder.Write(lineEnding);
                line = enumerator.Current;
                if (!line.IsEmpty)
                {
                    builder.Write(line.UnsafeSpan);
                }
            }

            var result = new U8String(builder.Written, skipValidation: true);

            builder.Dispose();
            return result;
        }

        return source;
    }
}
