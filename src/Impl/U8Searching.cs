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
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool Contains<T>(ReadOnlySpan<byte> value, T item)
    {
        Debug.Assert(item is byte or char or Rune or U8String or byte[]);

        if (typeof(T).IsValueType)
        {
            return item switch
            {
                byte b => value.Contains(b),

                char c => char.IsAscii(c)
                    ? value.Contains((byte)c)
                    : value.IndexOf(c.NonAsciiToUtf8(out _)) >= 0, // TODO: Optimize char conversion?

                Rune r => r.IsAscii
                    ? value.Contains((byte)r.Value)
                    : value.IndexOf(r.NonAsciiToUtf8(out _)) >= 0, // TODO: Dedup and forward to the next overload?

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int Count<T>(ReadOnlySpan<byte> value, T item)
    {
        Debug.Assert(item is byte or char or Rune or U8String or byte[]);

        if (typeof(T).IsValueType)
        {
            return item switch
            {
                byte b => value.Count(b),

                char c => char.IsAscii(c)
                    ? value.Count((byte)c)
                    : value.Count(c.NonAsciiToUtf8(out _)),

                Rune r => r.IsAscii
                    ? value.Count((byte)r.Value)
                    : value.Count(r.NonAsciiToUtf8(out _)),

                U8String str => value.Count(str),

                _ => 0
            };
        }
        else
        {
            return value.Count(Unsafe.As<T, byte[]>(ref item));
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
            return item.Length is 1 ? value.Count(item.AsRef()) : value.Count(item);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int IndexOf<T>(ReadOnlySpan<byte> value, T item)
    {
        Debug.Assert(item is byte or char or Rune or U8String or byte[]);

        if (typeof(T).IsValueType)
        {
            return item switch
            {
                byte b => value.IndexOf(b),

                char c => char.IsAscii(c)
                    ? value.IndexOf((byte)c)
                    : value.IndexOf(c.NonAsciiToUtf8(out _)),

                Rune r => r.IsAscii
                    ? value.IndexOf((byte)r.Value)
                    : value.IndexOf(r.NonAsciiToUtf8(out _)),

                U8String str => value.IndexOf(str.UnsafeSpan),

                _ => -1
            };
        }
        else
        {
            return value.IndexOf(Unsafe.As<T, byte[]>(ref item));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe int IndexOf<T>(ReadOnlySpan<byte> value, T item, U8Size size)
    {
        Debug.Assert(item is byte or char or Rune or U8String or byte[]);

        if (typeof(T).IsValueType)
        {
            if (item is byte b)
            {
                return value.IndexOf(b);
            }

            var _ = 0u;
            var bytes = _.AsBytes();

            fixed (byte* ptr = &bytes.AsRef())
            {
                switch (item)
                {
                    case char c:
                        // switch (size)
                        // {
                        //     case U8Size.Ascii:
                        //         return value.IndexOf((byte)c);

                        //     case U8Size.Two:
                        //         ptr[0] = (byte)(0xC0 | (c >> 6));
                        //         ptr[1] = (byte)(0x80 | (c & 0x3F));
                        //         break;

                        //     default:
                        //         ptr[0] = (byte)(0xE0 | (c >> 12));
                        //         ptr[1] = (byte)(0x80 | ((c >> 6) & 0x3F));
                        //         ptr[2] = (byte)(0x80 | (c & 0x3F));
                        //         break;
                        // }
                        // return value.IndexOf(bytes.SliceUnsafe(0, (int)size));

                        if (size is U8Size.Ascii)
                        {
                            return value.IndexOf((byte)c);
                        }
                        else if (size is U8Size.Two)
                        {
                            ptr[0] = (byte)(0xC0 | (c >> 6));
                            ptr[1] = (byte)(0x80 | (c & 0x3F));
                        }
                        else
                        {
                            ptr[0] = (byte)(0xE0 | (c >> 12));
                            ptr[1] = (byte)(0x80 | ((c >> 6) & 0x3F));
                            ptr[2] = (byte)(0x80 | (c & 0x3F));
                        }
                        return value.IndexOf(bytes.SliceUnsafe(0, (int)size));

                    case Rune r:
                        var scalar = (uint)r.Value;
                        if (size is U8Size.Ascii)
                        {
                            return value.IndexOf((byte)scalar);
                        }
                        else if (size is U8Size.Two)
                        {
                            ptr[0] = (byte)((scalar + (0b110u << 11)) >> 6);
                            ptr[1] = (byte)((scalar & 0x3Fu) + 0x80u);
                        }
                        else if (size is U8Size.Three)
                        {
                            ptr[0] = (byte)((scalar + (0b1110 << 16)) >> 12);
                            ptr[1] = (byte)(((scalar & (0x3Fu << 6)) >> 6) + 0x80u);
                            ptr[2] = (byte)((scalar & 0x3Fu) + 0x80u);
                        }
                        else
                        {
                            ptr[0] = (byte)((scalar + (0b11110 << 21)) >> 18);
                            ptr[1] = (byte)(((scalar & (0x3Fu << 12)) >> 12) + 0x80u);
                            ptr[2] = (byte)(((scalar & (0x3Fu << 6)) >> 6) + 0x80u);
                            ptr[3] = (byte)((scalar & 0x3Fu) + 0x80u);
                        }
                        return value.IndexOf(bytes.SliceUnsafe(0, (int)size));

                    case U8String str: return value.IndexOf(str);

                    default: return -1;
                }
            }
        }
        else
        {
            return value.IndexOf(Unsafe.As<byte[]>(item));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static (int Index, int Length) CountTBytesAndFind<T>(ReadOnlySpan<byte> value, T item)
    {
        Debug.Assert(item is byte or char or Rune or U8String or byte[]);

        if (typeof(T).IsValueType)
        {
            switch (item)
            {
                case byte b:
                    return (value.IndexOf(b), 1);

                case char c:
                    if (char.IsAscii(c))
                    {
                        return (value.IndexOf((byte)c), 1);
                    }
                    var charBytes = c.NonAsciiToUtf8(out _);
                    return (value.IndexOf(charBytes), charBytes.Length);

                case Rune r:
                    if (r.IsAscii)
                    {
                        return (value.IndexOf((byte)r.Value), 1);
                    }
                    var runeBytes = r.NonAsciiToUtf8(out _);
                    return (value.IndexOf(runeBytes), runeBytes.Length);

                case U8String str:
                    return (value.IndexOf(str), str.Length);

                default:
                    Debug.Fail("Unreachable");
                    return default;
            }
        }
        else
        {
            var bytes = Unsafe.As<T, byte[]>(ref item).AsSpan();
            return (value.IndexOf(bytes), bytes.Length);
        }
    }
}
