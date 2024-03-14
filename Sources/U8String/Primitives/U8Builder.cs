using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using U8.Abstractions;

namespace U8.Primitives;

public struct U8Builder : IU8Buffer
{
    // TODO: Turns out ArrayBufferWriter kind of sucks!
    // Replace it with a good custom implementation.
    // Notes:
    // - ArrayBufferWriter may reach quite a large size and be kept around
    // for indeterminate amount of time until the thread dies. Consider
    // thread-safe implementation with Gen2 GC callback that can drop
    // the reference when necessary;
    // - Consider a fallback where a buffer can be stolen from another
    // thread or shared pool in the case of multiple builders used at
    // the same time by the same thread or within async flow;
    // - Consider allocating the buffer outside of GC heap after crossing
    // a threshold (LOC? Some other one?), keep ptrs in a ConcurrentQueue?
    // --- disregard the above for now ---

    [ThreadStatic]
    static StrongBox<InlineU8Builder>? _tlv;

    StrongBox<InlineU8Builder>? _instance;

    internal readonly ref InlineU8Builder Handler => ref _instance!.Value;

    public readonly ReadOnlySpan<byte> Written => Handler.Written;

    readonly ReadOnlySpan<byte> IU8Buffer.Value => Written;

    public U8Builder()
    {
        _instance = Interlocked.Exchange(ref _tlv, null) ?? new();
        Handler.EnsureInitialized();
    }

    public U8Builder(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 0);

        _instance = Interlocked.Exchange(ref _tlv, null) ?? new();
        Handler.EnsureInitialized();
        Handler.EnsureCapacity(capacity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly U8Builder Append(bool value)
    {
        Handler.AppendFormatted(value);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly U8Builder Append(char value)
    {
        Handler.AppendFormatted(value);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly U8Builder Append(Rune value)
    {
        Handler.AppendFormatted(value);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly U8Builder Append(U8String value)
    {
        Handler.AppendFormatted(value);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly U8Builder Append(ReadOnlySpan<byte> value)
    {
        Handler.AppendFormatted(value);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly U8Builder Append<T>(T value)
        where T : IUtf8SpanFormattable
    {
        Handler.AppendFormatted(value);
        return this;
    }

    // TODO: IFormatProvider overload
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly U8Builder Append<T>(T value, ReadOnlySpan<char> format)
        where T : IUtf8SpanFormattable
    {
        Handler.AppendFormatted(value, format);
        return this;
    }

    // TODO: AppendLine methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal readonly void AppendBytes(ReadOnlySpan<byte> value)
    {
        Handler.AppendBytes(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        Handler.ArrayPoolSafeDispose();
        (_tlv, _instance) = (_instance, null);
    }

    public U8String Consume()
    {
        var result = new U8String(Written, skipValidation: true);
        Dispose();
        return result;
    }

    // Really wish there was a nuget with Box<T> and Ref<T> with
    // proxy generators to not have to write this boilerplate.
    [InterpolatedStringHandler]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly ref struct InterpolatedHandler
    {
        readonly ref InlineU8Builder _handler;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InterpolatedHandler(
            int literalLength,
            int formattedCount,
            U8Builder builder)
        {
            _handler = ref builder.Handler;
            _handler.EnsureCapacity(literalLength + (formattedCount * 12));
        }

        public void AppendLiteral([ConstantExpected] string s) => _handler.AppendLiteral(s);
        public void AppendLiteral(ReadOnlySpan<char> s) => _handler.AppendLiteral(s);

        public void AppendFormatted(bool value) => _handler.AppendFormatted(value);
        public void AppendFormatted(char value) => _handler.AppendFormatted(value);
        public void AppendFormatted(Rune value) => _handler.AppendFormatted(value);
        public void AppendFormatted(U8String value) => _handler.AppendFormatted(value);
        public void AppendFormatted(ReadOnlySpan<byte> value) => _handler.AppendFormatted(value);
        public void AppendFormatted(string? value) => _handler.AppendFormatted(value);
        public void AppendFormatted<T>(T value) => _handler.AppendFormatted(value);
        public void AppendFormatted<T>(T value, ReadOnlySpan<char> format)
            where T : IUtf8SpanFormattable => _handler.AppendFormatted(value, format);
    }
}

public static class U8BuilderExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8Builder Append<T>(this U8Builder builder, T value)
        where T : struct, Enum
    {
        builder.Handler.AppendFormatted(value);
        return builder;
    }

    public static U8Builder AppendLine<T>(this U8Builder builder, T value)
        where T : struct, Enum
    {
        throw new NotImplementedException();
    }

    // TODO: IFormatProvider overload
    public static U8Builder Append(
        this U8Builder builder,
        [InterpolatedStringHandlerArgument(nameof(builder))] U8Builder.InterpolatedHandler _)
    {
        // Roslyn unrolls string interpolation calls into handler ctor with specified args
        // and subsequent AppendLiteral and AppendFormat calls, so the actual appending to
        // builder has already happened by the time we get here.
        return builder;
    }
}
