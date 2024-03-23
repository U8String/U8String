using System.ComponentModel;

namespace U8.IO;

public static partial class U8WriteExtensions
{
    internal readonly struct StreamWriteable(Stream stream) : IWriteable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(ReadOnlySpan<byte> value)
        {
            stream.Write(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteDispose(ref InlineU8Builder builder)
        {
            stream.Write(builder.Written);
            builder.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask WriteAsync(ReadOnlyMemory<byte> value, CancellationToken ct)
        {
            return stream.WriteAsync(value, ct);
        }

        [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
        public async ValueTask WriteDisposeAsync(PooledU8Builder builder, CancellationToken ct)
        {
            await stream
                .WriteAsync(builder.WrittenMemory, ct)
                .ConfigureAwait(false);

            builder.Dispose();
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write(this Stream stream, ref InlineU8Builder handler)
    {
        WriteBuilder(new StreamWriteable(stream), ref handler);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<T>(this Stream stream, T value)
        where T : IUtf8SpanFormattable
    {
        WriteUtf8Formattable(new StreamWriteable(stream), value);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask WriteAsync(
        this Stream stream, PooledU8Builder handler, CancellationToken ct = default)
    {
        return WriteBuilderAsync(new StreamWriteable(stream), handler, ct);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask WriteAsync<T>(
        this Stream stream, T value, CancellationToken ct = default)
            where T : IUtf8SpanFormattable
    {
        return WriteUtf8FormattableAsync(new StreamWriteable(stream), value, ct);
    }

    public static void WriteLine(this Stream stream)
    {
        stream.Write(NewLine);
    }

    public static void WriteLine(this Stream stream, U8String value)
    {
        WriteLineSpan(new StreamWriteable(stream), value);
    }

    public static void WriteLine(this Stream stream, ReadOnlySpan<byte> value)
    {
        U8String.Validate(value);
        WriteLineSpan(new StreamWriteable(stream), value);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void WriteLine(this Stream stream, ref InlineU8Builder handler)
    {
        WriteLineBuilder(new StreamWriteable(stream), ref handler);
    }

    public static void WriteLine<T>(this Stream stream, T value)
        where T : IUtf8SpanFormattable
    {
        WriteLineUtf8Formattable(new StreamWriteable(stream), value);
    }

    public static ValueTask WriteLineAsync(
        this Stream stream, CancellationToken ct = default)
    {
        return stream.WriteAsync(U8Constants.NewLine, ct);
    }

    public static ValueTask WriteLineAsync(
        this Stream stream, U8String value, CancellationToken ct = default)
    {
        return WriteLineMemoryAsync(new StreamWriteable(stream), value, ct);
    }

    public static ValueTask WriteLineAsync(
        this Stream stream, ReadOnlyMemory<byte> value, CancellationToken ct = default)
    {
        U8String.Validate(value.Span);
        return WriteLineMemoryAsync(new StreamWriteable(stream), value, ct);
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask WriteLineAsync(
        this Stream stream, PooledU8Builder handler, CancellationToken ct = default)
    {
        return WriteLineBuilderAsync(new StreamWriteable(stream), handler, ct);
    }

    public static ValueTask WriteLineAsync<T>(
        this Stream stream, T value, CancellationToken ct = default)
            where T : IUtf8SpanFormattable
    {
        return WriteLineUtf8FormattableAsync(new StreamWriteable(stream), value, ct);
    }
}


public static partial class U8WriteEnumExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Write<T>(this Stream stream, T value)
        where T : struct, Enum
    {
        WriteEnum(new U8WriteExtensions.StreamWriteable(stream), value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask WriteAsync<T>(
        this Stream stream, T value, CancellationToken ct = default)
            where T : struct, Enum
    {
        return WriteEnumAsync(new U8WriteExtensions.StreamWriteable(stream), value, ct);
    }

    public static void WriteLine<T>(this Stream stream, T value)
        where T : struct, Enum
    {
        WriteLineEnum(new U8WriteExtensions.StreamWriteable(stream), value);
    }

    public static ValueTask WriteLineAsync<T>(
        this Stream stream, T value, CancellationToken ct = default)
            where T : struct, Enum
    {
        return WriteLineEnumAsync(new U8WriteExtensions.StreamWriteable(stream), value, ct);
    }
}
