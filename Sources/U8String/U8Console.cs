using System.Buffers;

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

    public static void Write(ref InterpolatedU8StringHandler handler)
    {
        Out.Write(handler.Written);
        handler.Dispose();
    }

    public static void WriteLine(U8String value)
    {
        byte[]? rented = null;
        Unsafe.SkipInit(out InlineBuffer128 stack);

        var newline = NewLine;
        var length = value.Length + newline.Length;

        var buffer = length <= InlineBuffer128.Length
            ? stack : (rented = ArrayPool<byte>.Shared.Rent(length)).AsSpan();

        value.AsSpan().CopyToUnsafe(ref buffer.AsRef());
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

    public static void WriteLine(ref InterpolatedU8StringHandler handler)
    {
        handler.AppendBytesInlined(NewLine);
        Out.Write(handler.Written);
        handler.Dispose();
    }
}
