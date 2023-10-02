using System.Text;

namespace U8Primitives;

public readonly partial struct U8String
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool TryFormatPresized<T>(
        ReadOnlySpan<char> format, T value, IFormatProvider? provider, out U8String result)
            where T : IUtf8SpanFormattable
    {
        var length = GetFormattedLength<T>();
        var buffer = new byte[length]; // Most cases will be shorter than this, no need to add extra null terminator
        var success = value.TryFormat(buffer, out length, format, provider);

        result = new(buffer, 0, length);
        return success;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int GetFormattedLength<T>() where T : IUtf8SpanFormattable
    {
        if (typeof(T) == typeof(byte)) return 8;
        if (typeof(T) == typeof(char)) return 8;
        if (typeof(T) == typeof(Rune)) return 8;
        if (typeof(T) == typeof(sbyte)) return 8;
        if (typeof(T) == typeof(ushort)) return 8;
        if (typeof(T) == typeof(short)) return 8;
        if (typeof(T) == typeof(uint)) return 16;
        if (typeof(T) == typeof(int)) return 16;
        if (typeof(T) == typeof(ulong)) return 24;
        if (typeof(T) == typeof(long)) return 24;
        if (typeof(T) == typeof(float)) return 16;
        if (typeof(T) == typeof(double)) return 24;
        if (typeof(T) == typeof(decimal)) return 32;
        if (typeof(T) == typeof(DateTime)) return 32;
        if (typeof(T) == typeof(DateTimeOffset)) return 40;
        if (typeof(T) == typeof(TimeSpan)) return 24;
        if (typeof(T) == typeof(Guid)) return 40;
        else return 32;
    }

    static U8String FormatUnsized<T>(
        ReadOnlySpan<char> format, T value, IFormatProvider? provider)
            where T : IUtf8SpanFormattable
    {
        // TODO: Additional length-resolving heuristics or a stack-allocated into arraypool buffer
        int length;
        var buffer = new byte[64];
        while (!value.TryFormat(buffer, out length, format, provider))
        {
            buffer = new byte[buffer.Length * 2];
        }

        return new(buffer, 0, length);
    }
}
