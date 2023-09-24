using System.Diagnostics;
using System.Text;

namespace U8Primitives;

internal static class U8Manipulation
{
    internal static U8String ConcatUnchecked(ReadOnlySpan<byte> left, byte right)
    {
        var length = left.Length + 1;
        var value = new byte[length];
        var span = value.AsSpan();

        left.CopyTo(span.SliceUnsafe(0, left.Length));
        span.AsRef(length - 1) = right;

        return new U8String(value, 0, length);
    }

    internal static U8String ConcatUnchecked(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        var length = left.Length + right.Length;
        var value = new byte[length];

        left.CopyTo(value.SliceUnsafe(0, left.Length));
        right.CopyTo(value.SliceUnsafe(left.Length, right.Length));

        return new U8String(value, 0, length);
    }

    // Contract:
    // - values.Length is greater than 0
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
    // - values.Length is greater than 0
    // - separator is a non-ascii byte, char, Rune or U8Scalar
    internal static U8String JoinUnchecked(ReadOnlySpan<byte> separator, ReadOnlySpan<U8String> values)
    {
        Debug.Assert(values.Length > 1);
        Debug.Assert(separator.Length > 0);

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
            if (firstReplace < 0)
            {
                return source;
            }

            var replaced = new byte[source.Length];
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

        return default;
    }

    internal static U8String Replace(U8String source, char oldValue, char newValue)
    {
        if (char.IsSurrogate(oldValue) || char.IsSurrogate(newValue))
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        return char.IsAscii(oldValue) && char.IsAscii(newValue)
            ? Replace(source, (byte)oldValue, (byte)newValue, validate: false)
            : ReplaceCore(
                source,
                U8Scalar.Create(oldValue).AsSpan(),
                U8Scalar.Create(newValue).AsSpan());
    }

    internal static U8String Replace(U8String source, Rune oldValue, Rune newValue)
    {
        return oldValue.IsAscii && newValue.IsAscii
            ? Replace(source, (byte)oldValue.Value, (byte)newValue.Value, validate: false)
            : ReplaceCore(
                source,
                U8Scalar.Create(oldValue).AsSpan(),
                U8Scalar.Create(newValue).AsSpan());
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
    /// <para>- <paramref name="oldValue"/> and <paramref name="newValue"/> lengths are not 1</para>
    /// </summary>
    internal static U8String ReplaceCore(
        U8String source,
        ReadOnlySpan<byte> oldValue,
        ReadOnlySpan<byte> newValue,
        bool validate = true)
    {
        Debug.Assert(!source.IsEmpty);
        Debug.Assert(!oldValue.IsEmpty);
        Debug.Assert(!newValue.IsEmpty);
        Debug.Assert(oldValue.Length is not 1 && newValue.Length is not 1);

        var count = source.UnsafeSpan.Count(oldValue);
        if (count is not 0)
        {
            var length = source.Length - (oldValue.Length * count) + (newValue.Length * count);

            var result = new byte[length];
            var destination = result.AsSpan();

            foreach (var segment in new U8RefSplit(source, oldValue))
            {
                segment.AsSpan().CopyTo(destination.SliceUnsafe(0, segment.Length));
                destination = destination.SliceUnsafe(segment.Length);
                if (destination.Length is 0)
                {
                    break;
                }

                newValue.CopyTo(destination.SliceUnsafe(0, newValue.Length));
                destination = destination.SliceUnsafe(newValue.Length);
            }

            if (validate)
            {
                U8String.Validate(destination);
            }

            return new(result, 0, result.Length);
        }

        return source;
    }

    internal static U8String Remove(U8String source, ReadOnlySpan<byte> value, bool validate = true)
    {
        var count = source.AsSpan().Count(value);
        if (count is not 0)
        {
            var result = new byte[source.Length - (value.Length * count)];
            var destination = result.AsSpan();

            foreach (var segment in new U8RefSplit(source, value))
            {
                segment.AsSpan().CopyTo(destination.SliceUnsafe(0, segment.Length));
                destination = destination.SliceUnsafe(segment.Length);
            }

            if (validate)
            {
                U8String.Validate(destination);
            }
            return new(result, 0, result.Length);
        }

        return source;
    }

    internal static U8String Remove(U8String source, byte value)
    {
        var count = source.AsSpan().Count(value);
        if (count is not 0)
        {
            var result = new byte[source.Length - count];
            var destination = result.AsSpan();

            foreach (var segment in new U8Split<byte>(source, value))
            {
                segment.AsSpan().CopyTo(destination.SliceUnsafe(0, segment.Length));
                destination = destination.SliceUnsafe(segment.Length);
            }

            if (!U8Info.IsAsciiByte(value))
            {
                U8String.Validate(destination);
            }

            return new(result, 0, result.Length);
        }

        return source;
    }
}
