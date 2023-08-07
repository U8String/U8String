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
        return item.Length is 1 ? value.Count(item.AsRef()) : value.Count(item);
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

                U8String str => value.IndexOf(str),

                _ => -1
            };
        }
        else
        {
            return value.IndexOf(Unsafe.As<T, byte[]>(ref item));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int IndexOf(ReadOnlySpan<byte> value, ReadOnlySpan<byte> item)
    {
        return item.Length is 1 ? value.IndexOf(item.AsRef()) : value.IndexOf(item);
    }
}
