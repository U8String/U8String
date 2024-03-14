using System.ComponentModel;

namespace U8.IO;

public static partial class U8WriteExtensions
{
    internal readonly struct StreamWriteable(Stream stream) : IWriteable
    {
        public void Write(ReadOnlySpan<byte> value)
        {
            stream.Write(value);
        }

        public void WriteDispose(ref InlineU8Builder builder)
        {
            stream.Write(builder.Written);
            builder.Dispose();
        }

        public ValueTask WriteAsync(ReadOnlyMemory<byte> value, CancellationToken ct)
        {
            return stream.WriteAsync(value, ct);
        }

        public async ValueTask WriteDisposeAsync(PooledU8Builder builder, CancellationToken ct)
        {
            await stream
                .WriteAsync(builder.WrittenMemory, ct)
                .ConfigureAwait(false);

            builder.Dispose();
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Write(this Stream stream, ref InlineU8Builder handler)
    {
        Write(new StreamWriteable(stream), ref handler);
    }

    public static void Write<T>(this Stream stream, T value)
        where T : IUtf8SpanFormattable
    {
        Write(new StreamWriteable(stream), value);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask WriteAsync(
        this Stream stream, PooledU8Builder handler, CancellationToken ct = default)
    {
        return WriteAsync(new StreamWriteable(stream), handler, ct);
    }

    public static ValueTask WriteAsync<T>(
        this Stream stream, T value, CancellationToken ct = default)
            where T : IUtf8SpanFormattable
    {
        return WriteAsync(new StreamWriteable(stream), value, ct);
    }

    public static void WriteLine(this Stream stream)
    {
        stream.Write(NewLine);
    }

    public static void WriteLine(this Stream stream, U8String value)
    {
        WriteLine(new StreamWriteable(stream), value);
    }

    public static void WriteLine(this Stream stream, ReadOnlySpan<byte> value)
    {
        U8String.Validate(value);
        WriteLine(new StreamWriteable(stream), value);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void WriteLine(this Stream stream, ref InlineU8Builder handler)
    {
        WriteLine(new StreamWriteable(stream), ref handler);
    }

    public static void WriteLine<T>(this Stream stream, T value)
        where T : IUtf8SpanFormattable
    {
        WriteLine(new StreamWriteable(stream), value);
    }

    public static ValueTask WriteLineAsync(
        this Stream stream, CancellationToken ct = default)
    {
        return stream.WriteAsync(U8Constants.NewLine, ct);
    }

    public static ValueTask WriteLineAsync(
        this Stream stream, U8String value, CancellationToken ct = default)
    {
        return WriteLineAsync(new StreamWriteable(stream), value, ct);
    }

    public static ValueTask WriteLineAsync(
        this Stream stream, ReadOnlyMemory<byte> value, CancellationToken ct = default)
    {
        U8String.Validate(value.Span);
        return WriteLineAsync(new StreamWriteable(stream), value, ct);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask WriteLineAsync(
        this Stream stream, PooledU8Builder handler, CancellationToken ct = default)
    {
        return WriteLineAsync(new StreamWriteable(stream), handler, ct);
    }

    public static ValueTask WriteLineAsync<T>(
        this Stream stream, T value, CancellationToken ct = default)
            where T : IUtf8SpanFormattable
    {
        return WriteLineAsync(new StreamWriteable(stream), value, ct);
    }
}


public static partial class U8WriteEnumExtensions
{
    public static void Write<T>(this Stream stream, T value)
        where T : struct, Enum
    {
        Write(new U8WriteExtensions.StreamWriteable(stream), value);
    }

    public static ValueTask WriteAsync<T>(
        this Stream stream, T value, CancellationToken ct = default)
            where T : struct, Enum
    {
        return WriteAsync(new U8WriteExtensions.StreamWriteable(stream), value, ct);
    }

    public static void WriteLine<T>(this Stream stream, T value)
        where T : struct, Enum
    {
        WriteLine(new U8WriteExtensions.StreamWriteable(stream), value);
    }

    public static ValueTask WriteLineAsync<T>(
        this Stream stream, T value, CancellationToken ct = default)
            where T : struct, Enum
    {
        return WriteLineAsync(new U8WriteExtensions.StreamWriteable(stream), value, ct);
    }
}
