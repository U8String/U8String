using System.Buffers;
using System.ComponentModel;

namespace U8.IO;

// TODO: culture-sensitive and format-specific overloads
public static class U8StreamExtensions
{
    static ReadOnlySpan<byte> NewLine
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => OperatingSystem.IsWindows() ? "\r\n"u8 : "\n"u8;
    }

    public static void Write<T>(this Stream stream, T value)
        where T : IUtf8SpanFormattable
    {
        if (value is U8String s)
        {
            stream.Write(s);
            return;
        }

        var builder = new InlineU8Builder();
        builder.AppendFormatted(value);
        stream.Write(builder.Written);
        builder.Dispose();
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Write(this Stream stream, ref InlineU8Builder handler)
    {
        stream.Write(handler.Written);
        handler.Dispose();
    }

    // public static async ValueTask WriteAsync<T>(
    //     this Stream stream, T value, CancellationToken cancellationToken = default)
    //         where T : IUtf8SpanFormattable
    // {
    //     var builder = new MemoryBuilder();
    //     builder.Write(value, default, CultureInfo.InvariantCulture);
    //     builder.Write(NewLine);

    //     await stream.WriteAsync(builder.Written, cancellationToken).ConfigureAwait(false);
    //     builder.Dispose();
    // }

    // public static ValueTask WriteAsync(
    //     this Stream stream, ref InlineU8Builder handler, CancellationToken cancellationToken = default)
    // {
    //     // TODO: It's a good question whether to make *yet another* interpolation handler type
    //     // to avoid byref requirement of InlineU8Builder or whether just copying to MemoryBuilder
    //     // is an acceptable cost?
    //     throw new NotImplementedException();
    // }

    public static void WriteLine(this Stream stream)
    {
        stream.Write(NewLine);
    }

    public static void WriteLine(this Stream stream, U8String value)
    {
        WriteLineUnchecked(stream, value);
    }

    public static void WriteLine(this Stream stream, ReadOnlySpan<byte> value)
    {
        U8String.Validate(value);
        WriteLineUnchecked(stream, value);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void WriteLine(this Stream stream, ref InlineU8Builder handler)
    {
        handler.AppendBytesInlined(NewLine);
        stream.Write(handler.Written);
        handler.Dispose();
    }

    internal static void WriteLineUnchecked(Stream stream, ReadOnlySpan<byte> value)
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

        stream.Write(buffer.SliceUnsafe(0, length));

        if (rented != null)
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    // public static ValueTask WriteLineAsync(this Stream stream, CancellationToken cancellationToken = default)
    // {
    //     return stream.WriteAsync(U8Constants.NewLine, cancellationToken);
    // }

    // internal static ValueTask WriteLineAsyncUnchecked(
    //     Stream stream, ReadOnlyMemory<byte> value, CancellationToken cancellationToken = default)
    // {
    //     throw new NotImplementedException();
    // }
}

public static class U8StreamEnumExtensions
{
    public static void Write<T>(this Stream stream, T value)
        where T : struct, Enum
    {
        // The ToU8String call will almost never allocate, so we can just defer to U8EnumExtensions.
        stream.Write(value.ToU8String());
    }

    // public static ValueTask WriteAsync<T>(
    //     this Stream stream, T value, CancellationToken cancellationToken = default)
    //         where T : struct, Enum
    // {
    //     return stream.WriteAsync(value.ToU8String(), cancellationToken);
    // }

    public static void WriteLine<T>(this Stream stream, T value)
        where T : struct, Enum
    {
        U8StreamExtensions.WriteLineUnchecked(stream, value.ToU8String());
    }

    // public static ValueTask WriteLineAsync<T>(
    //     this Stream stream, T value, CancellationToken cancellationToken = default)
    //         where T : struct, Enum
    // {
    //     return U8StreamExtensions.WriteLineAsyncUnchecked(stream, value.ToU8String(), cancellationToken);
    // }
}