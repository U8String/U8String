using System.Diagnostics;
using System.Text;

namespace U8Primitives;

// TODO: Better name?
internal static class U8Searching
{
    /// <summary>
    /// Returns the index of the first occurrence of a specified value in a span.
    /// </summary>
    /// <remarks>
    /// Designed to be inlined into the caller and optimized away on constants.
    /// <para>
    /// Contract: when T is char and a surrogate, the return value is false.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool Contains<T>(ReadOnlySpan<byte> value, T item)
    {
        Debug.Assert(item is byte or char or Rune or U8String or byte[] or U8Scalar);

        if (typeof(T).IsValueType)
        {
            return item switch
            {
                byte b => value.Contains(b),

                char c => char.IsAscii(c)
                    ? value.Contains((byte)c)
                    : !char.IsSurrogate(c)
                        && value.IndexOf(U8Scalar.Create(c, checkAscii: false).AsSpan()) >= 0,

                Rune r => r.IsAscii
                    ? value.Contains((byte)r.Value)
                    : value.IndexOf(U8Scalar.Create(r, checkAscii: false).AsSpan()) >= 0,

                U8Scalar s => s.Size is 1
                    ? value.Contains(s.B0)
                    : value.IndexOf(s.AsSpan()) >= 0,

                U8String str => value.IndexOf(str) >= 0,

                _ => false
            };
        }
        else
        {
            return value.IndexOf(Unsafe.As<T, byte[]>(ref item)) >= 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool Contains(ReadOnlySpan<byte> value, ReadOnlySpan<byte> item)
    {
        return item.Length is 1 ? value.Contains(item.AsRef()) : value.IndexOf(item) >= 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool SplitContains<T>(
        ReadOnlySpan<byte> value,
        T separator,
        ReadOnlySpan<byte> item)
    {
        return !Contains(item, separator) && Contains(value, item);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool SplitContains(
        ReadOnlySpan<byte> value,
        ReadOnlySpan<byte> separator,
        ReadOnlySpan<byte> item)
    {
        // When the item we are looking for contains the separator, it means that it will
        // never be found in the split since it would be pointing to the split boundary.
        return !Contains(item, separator) && Contains(value, item);
    }

    /// <summary>
    /// Contract: when T is char, it must never be a surrogate.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Count<T>(U8String value, T item)
    {
        Debug.Assert(!value.IsEmpty);
        Debug.Assert(item is not char i || !char.IsSurrogate(i));
        Debug.Assert(item is byte or char or Rune or U8String or byte[] or U8Scalar);

        if (typeof(T).IsValueType)
        {
            return item switch
            {
                byte b => value.UnsafeSpan.Count(b),

                char c => char.IsAscii(c)
                    ? value.UnsafeSpan.Count((byte)c)
                    : value.UnsafeSpan.Count(U8Scalar.Create(c, checkAscii: false).AsSpan()),

                Rune r => r.IsAscii
                    ? value.UnsafeSpan.Count((byte)r.Value)
                    : value.UnsafeSpan.Count(U8Scalar.Create(r, checkAscii: false).AsSpan()),

                U8Scalar s => value.UnsafeSpan.Count(s.AsSpan()),

                U8String str => value.UnsafeSpan.Count(str),

                _ => 0
            };
        }
        else
        {
            return value.UnsafeSpan.Count(Unsafe.As<T, byte[]>(ref item));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Count(ReadOnlySpan<byte> value, ReadOnlySpan<byte> item)
    {
        //return item.Length is 1 ? value.Count(item.AsRef()) : value.Count(item);
        return value.Count(item); // This already has internal check for Length is 1
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Count(ReadOnlySpan<byte> value, ReadOnlySpan<byte> item, U8SplitOptions options)
    {
        if (options is U8SplitOptions.None)
        {
            return Count(value, item);
        }

        return CountSlow(value, item, options);
    }

    internal static int Count<T>(ReadOnlySpan<byte> value, T item, U8SplitOptions options)
    {
        Debug.Assert(options != U8SplitOptions.None);
        throw new NotImplementedException();
    }

    internal static int CountSlow(ReadOnlySpan<byte> value, ReadOnlySpan<byte> item, U8SplitOptions options)
    {
        Debug.Assert(options != U8SplitOptions.None);
        throw new NotImplementedException();
    }

    /// <summary>
    /// Contract: when T is char, it must never be a surrogate.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int IndexOf<T>(ReadOnlySpan<byte> value, T item)
    {
        Debug.Assert(item is not char i || !char.IsSurrogate(i));
        Debug.Assert(item is byte or char or Rune or U8String or byte[] or U8Scalar);

        if (typeof(T).IsValueType)
        {
            return item switch
            {
                byte b => value.IndexOf(b),

                char c => char.IsAscii(c)
                    ? value.IndexOf((byte)c)
                    : value.IndexOf(U8Scalar.Create(c, checkAscii: false).AsSpan()),

                Rune r => r.IsAscii
                    ? value.IndexOf((byte)r.Value)
                    : value.IndexOf(U8Scalar.Create(r, checkAscii: false).AsSpan()),

                U8Scalar s => s.Size is 1 ? value.IndexOf(s.B0) : value.IndexOf(s.AsSpan()),

                U8String str => value.IndexOf(str.UnsafeSpan),

                _ => -1
            };
        }
        else
        {
            return value.IndexOf(Unsafe.As<T, byte[]>(ref item));
        }
    }
}
