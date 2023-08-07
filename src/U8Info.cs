using System.Diagnostics;
using System.Text;

namespace U8Primitives;

internal static class U8Info
{
    // From https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Text/Unicode/Utf8Utility.Helpers.cs#L393C40-L393C40
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsContinuationByte(in byte value)
    {
        return (sbyte)value < -64;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsAsciiByte(in byte value)
    {
        return value <= 0x7F;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static U8Size GetSize<T>(T value)
    {
        Debug.Assert(value is byte or char or Rune or U8String or byte[]);

        if (typeof(T).IsValueType)
        {
            return value switch
            {
                byte => U8Size.Ascii,
                char c => c switch
                {
                    <= (char)0x7F => U8Size.Ascii,
                    <= (char)0x7FF => U8Size.Two,
                    _ => U8Size.Three
                },
                Rune r => r.Value switch
                {
                    <= 0x7F => U8Size.Ascii,
                    <= 0x7FF => U8Size.Two,
                    <= 0xFFFF => U8Size.Three,
                    _ => U8Size.Four
                },
                U8String str => (U8Size)str.Length,
                _ => 0
            };
        }
        else
        {
            return (U8Size)Unsafe.As<byte[]>(value).Length;
        }
    }
}
