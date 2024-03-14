using U8.Serialization;

namespace U8.IO;

// Overloads order:
// Write(interpolation)
// Write<T>(formattable)
// WriteAsync(interpolation)
// WriteAsync<T>(formattable)
// WriteLine(u8string)
// WriteLine(u8bytes)
// WriteLine(interpolation)
// WriteLine<T>(formattable)
// TODO: culture-sensitive and format-specific overloads
public static partial class U8WriteExtensions
{
    internal interface IWriteable
    {
        void Write(ReadOnlySpan<byte> value);
        void WriteDispose(ref InlineU8Builder builder);

        ValueTask WriteAsync(ReadOnlyMemory<byte> value, CancellationToken ct);
        ValueTask WriteDisposeAsync(PooledU8Builder builder, CancellationToken ct);
    }

    internal static ReadOnlySpan<byte> NewLine
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => OperatingSystem.IsWindows() ? "\r\n"u8 : "\n"u8;
    }

    static void Write<T>(T destination, ref InlineU8Builder builder)
        where T : IWriteable
    {
        destination.WriteDispose(ref builder);
    }

    static void Write<T, U>(T destination, U value)
        where T : IWriteable
        where U : IUtf8SpanFormattable
    {
        var builder = new InlineU8Builder();
        builder.AppendFormatted(value);
        destination.WriteDispose(ref builder);
    }

    static ValueTask WriteAsync<T>(T destination, PooledU8Builder builder, CancellationToken ct)
        where T : IWriteable
    {
        return destination.WriteDisposeAsync(builder, ct);
    }

    static ValueTask WriteAsync<T, U>(T destination, U value, CancellationToken ct)
        where T : IWriteable
        where U : IUtf8SpanFormattable
    {
        var builder = new PooledU8Builder();
        builder.AppendFormatted(value);
        builder.AppendBytesInlined(NewLine);
        return destination.WriteDisposeAsync(builder, ct);
    }

    static void WriteLine<T>(T destination, ReadOnlySpan<byte> value)
        where T : IWriteable
    {
        var builder = new InlineU8Builder(value.Length + NewLine.Length);
        builder.AppendBytesUnchecked(value);
        builder.AppendBytesUnchecked(NewLine);
        destination.WriteDispose(ref builder);
    }

    static void WriteLine<T>(T destination, ref InlineU8Builder builder)
        where T : IWriteable
    {
        builder.AppendBytesInlined(NewLine);
        destination.WriteDispose(ref builder);
    }

    static void WriteLine<T, U>(T destination, U value)
        where T : IWriteable
        where U : IUtf8SpanFormattable
    {
        var builder = new InlineU8Builder();
        builder.AppendFormatted(value);
        builder.AppendBytesInlined(NewLine);
        destination.WriteDispose(ref builder);
    }

    static ValueTask WriteLineAsync<T>(T destination, ReadOnlyMemory<byte> value, CancellationToken ct)
        where T : IWriteable
    {
        var builder = new PooledU8Builder(value.Length + NewLine.Length);
        builder.AppendBytesUnchecked(value.Span);
        builder.AppendBytesUnchecked(NewLine);
        return destination.WriteDisposeAsync(builder, ct);
    }

    static ValueTask WriteLineAsync<T>(T destination, PooledU8Builder builder, CancellationToken ct)
        where T : IWriteable
    {
        builder.AppendBytesInlined(NewLine);
        return destination.WriteDisposeAsync(builder, ct);
    }

    static ValueTask WriteLineAsync<T, U>(T destination, U value, CancellationToken ct)
        where T : IWriteable
        where U : IUtf8SpanFormattable
    {
        var builder = new PooledU8Builder();
        builder.AppendFormatted(value);
        builder.AppendBytesInlined(NewLine);
        return destination.WriteDisposeAsync(builder, ct);
    }
}

public static partial class U8WriteEnumExtensions
{
    static void Write<T, U>(T destination, U value)
        where T : U8WriteExtensions.IWriteable
        where U : struct, Enum
    {
        destination.Write(value.ToU8String());
    }

    static ValueTask WriteAsync<T, U>(T destination, U value, CancellationToken ct)
        where T : U8WriteExtensions.IWriteable
        where U : struct, Enum
    {
        return destination.WriteAsync(value.ToU8String(), ct);
    }

    static void WriteLine<T, U>(T destination, U value)
        where T : U8WriteExtensions.IWriteable
        where U : struct, Enum
    {
        var formatted = value.ToU8String();
        var newline = U8WriteExtensions.NewLine;

        var builder = new InlineU8Builder(formatted.Length + newline.Length);
        builder.AppendBytesUnchecked(formatted);
        builder.AppendBytesUnchecked(newline);
        destination.WriteDispose(ref builder);
    }

    static ValueTask WriteLineAsync<T, U>(T destination, U value, CancellationToken ct)
        where T : U8WriteExtensions.IWriteable
        where U : struct, Enum
    {
        var formatted = value.ToU8String();
        var newline = U8WriteExtensions.NewLine;

        var builder = new PooledU8Builder(formatted.Length + newline.Length);
        builder.AppendBytesUnchecked(formatted);
        builder.AppendBytesUnchecked(newline);
        return destination.WriteDisposeAsync(builder, ct);
    }
}
