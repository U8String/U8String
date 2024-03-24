using System.Buffers;
using System.ComponentModel;

using U8.IO;

namespace U8;

public static class U8Console
{
    static readonly Stream Out = Console.OpenStandardOutput();

    static ReadOnlySpan<byte> NewLine
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => OperatingSystem.IsWindows() ? "\r\n"u8 : "\n"u8;
    }

    public static void Write(U8String value)
    {
        Out.Write(value);
    }

    public static void Write(ReadOnlySpan<byte> value)
    {
        U8String.Validate(value);
        Out.Write(value);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Write(ref InlineU8Builder handler)
    {
        Out.Write(ref handler);
    }

    public static void Write<T>(T value)
        where T : IUtf8SpanFormattable
    {
        Out.Write(value);
    }

    public static void WriteLine()
    {
        Out.Write(NewLine);
    }

    public static void WriteLine(U8String value)
    {
        Out.WriteLine(value);
    }

    public static void WriteLine(ReadOnlySpan<byte> value)
    {
        Out.WriteLine(value);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void WriteLine(ref InlineU8Builder handler)
    {
        Out.WriteLine(ref handler);
    }

    public static void WriteLine<T>(T value)
        where T : IUtf8SpanFormattable
    {
        Out.WriteLine(value);
    }
}
