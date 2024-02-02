using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace U8.Primitives;

public readonly struct U8Builder
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
    static Box<InterpolatedU8StringHandler>? _cached;
    readonly Box<InterpolatedU8StringHandler> _local;

    internal ref InterpolatedU8StringHandler Handler => ref _local.Ref;

    public ReadOnlySpan<byte> Written => _local.Ref.Written;

    public U8Builder()
    {
        _local = Interlocked.Exchange(ref _cached, null) ?? new();
    }

    public U8Builder(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 0);

        var local = Interlocked.Exchange(ref _cached, null) ?? new();
        local.Ref.EnsureCapacity(capacity);
        _local = local;
    }

    public U8Builder Append(bool value)
    {
        _local.Ref.AppendFormatted(value);
        return this;
    }

    public U8Builder Append(char value)
    {
        _local.Ref.AppendFormatted(value);
        return this;
    }

    public U8Builder Append(Rune value)
    {
        _local.Ref.AppendFormatted(value);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8Builder Append(U8String value)
    {
        _local.Ref.AppendFormatted(value);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8Builder Append(ReadOnlySpan<byte> value)
    {
        _local.Ref.AppendFormatted(value);
        return this;
    }

    public U8Builder Append<T>(T value)
        where T : IUtf8SpanFormattable
    {
        _local.Ref.AppendFormatted(value);
        return this;
    }

    public U8Builder Append<T>(T value, ReadOnlySpan<char> format)
        where T : IUtf8SpanFormattable
    {
        _local.Ref.AppendFormatted(value, format);
        return this;
    }

    // TODO: IFormatProvider overload

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8Builder AppendLiteral([ConstantExpected] string value)
    {
        _local.Ref.AppendLiteral(value);
        return this;
    }

    public void Dispose()
    {
        _local.Ref.ArrayPoolSafeDispose();
        _cached = _local;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String Consume()
    {
        var deref = this;
        var result = new U8String(deref.Written, skipValidation: true);
        deref.Dispose();
        return result;
    }

    // Really wish there was a nuget with Box<T> and Ref<T> with
    // proxy generators to not have to write this boilerplate.
    [InterpolatedStringHandler]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly ref struct InterpolatedHandler
    {
        readonly ref InterpolatedU8StringHandler _handler;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InterpolatedHandler(
            int literalLength,
            int formattedCount,
            U8Builder builder)
        {
            _handler = ref builder._local.Ref;
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
    public static U8Builder Append<T>(this U8Builder builder, T value)
        where T : struct, Enum
    {
        builder.Handler.AppendFormatted(value);
        return builder;
    }

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
