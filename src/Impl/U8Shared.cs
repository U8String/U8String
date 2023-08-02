using System.Diagnostics;
using System.Text;

namespace U8Primitives;

// TODO: Better name?
static class U8Shared
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
                    : value.IndexOf(r.ToUtf8(out _)) >= 0,

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
        return item.Length is 1 ? value.Contains(item[0]) : value.IndexOf(item) >= 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int CountSegments<T>(ReadOnlySpan<byte> value, T separator)
    {
        Debug.Assert(separator is byte or char or Rune or U8String or byte[]);

        if (typeof(T).IsValueType)
        {
            return separator switch
            {
                byte b => value.Count(b),

                char c => char.IsAscii(c)
                    ? value.Count((byte)c)
                    : value.Count(c.NonAsciiToUtf8(out _)),

                Rune r => r.IsAscii
                    ? value.Count((byte)r.Value)
                    : value.Count(r.ToUtf8(out _)),

                U8String str => value.Count(str),

                _ => 0
            };
        }
        else
        {
            return value.Count(Unsafe.As<T, byte[]>(ref separator));
        }
    }

    internal static int CountSegments<T>(ReadOnlySpan<byte> value, T separator, U8SplitOptions options)
    {
        Debug.Assert(options != U8SplitOptions.None);
        throw new NotImplementedException();
    }
}
