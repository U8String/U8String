using System.Buffers;
using System.ComponentModel;

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
    public static void Write(ref InterpolatedU8StringHandler handler)
    {
        Out.Write(handler.Written);
        handler.Dispose();
    }

    public static void Write<T>(T value)
        where T : IUtf8SpanFormattable
    {
        var handler = new InterpolatedU8StringHandler();
        handler.AppendFormatted(value);
        Out.Write(handler.Written);
    }

    public static void WriteLine()
    {
        Out.Write(NewLine);
    }

    public static void WriteLine(U8String value)
    {
        WriteLineUnchecked(value);
    }

    public static void WriteLine(ReadOnlySpan<byte> value)
    {
        U8String.Validate(value);
        WriteLineUnchecked(value);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void WriteLine(ref InterpolatedU8StringHandler handler)
    {
        handler.AppendBytesInlined(NewLine);
        Out.Write(handler.Written);
        handler.Dispose();
    }

    static void WriteLineUnchecked(ReadOnlySpan<byte> value)
    {
        byte[]? rented = null;
        Unsafe.SkipInit(out InlineBuffer128 stack);

        var newline = NewLine;
        var length = value.Length + newline.Length;

        var buffer = length <= InlineBuffer128.Length
            ? stack : (rented = ArrayPool<byte>.Shared.Rent(length)).AsSpan();

        value.CopyToUnsafe(ref buffer.AsRef());
        if (OperatingSystem.IsWindows())
        {
            buffer.AsRef(value.Length) = (byte)'\r';
            buffer.AsRef(value.Length + 1) = (byte)'\n';
        }
        else
        {
            buffer.AsRef(value.Length) = (byte)'\n';
        }

        Out.Write(buffer.SliceUnsafe(0, length));

        if (rented != null)
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }
}
