using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;

using U8.Abstractions;
using U8.IO;
using U8.Shared;

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
        (_instance, _tlv) = (_tlv ?? new(), null);
        Handler.EnsureInitialized();
    }

    public U8Builder(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 0);

        (_instance, _tlv) = (_tlv ?? new(), null);
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

    /// <summary>
    /// Consumes the written bytes by returning them as a new <see cref="U8String"/> and resets the builder.
    /// </summary> 
    /// <remarks>
    /// The builder can be reused after calling this method. Disposing the builder after calling this method
    /// is not necessary. 
    /// </remarks>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public U8String Consume()
    {
        var result = new U8String(Written, skipValidation: true);
        Dispose();
        return result;
    }

    public readonly void Reset() => Handler.Reset();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        Handler.ArrayPoolSafeDispose();
        (_tlv, _instance) = (_instance, null);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendLiteral([ConstantExpected] string s) => U8Interpolation.AppendLiteral(ref _handler, s);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendFormatted(bool value) => U8Interpolation.AppendFormatted(ref _handler, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendFormatted(char value) => U8Interpolation.AppendFormatted(ref _handler, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendFormatted(Rune value) => U8Interpolation.AppendFormatted(ref _handler, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendFormatted(U8String value) => U8Interpolation.AppendFormatted(ref _handler, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendFormatted(U8String? value) => U8Interpolation.AppendFormatted(ref _handler, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendFormatted(ReadOnlySpan<byte> value) => U8Interpolation.AppendFormatted(ref _handler, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendFormatted(string? value) => U8Interpolation.AppendFormatted(ref _handler, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendFormatted(ReadOnlySpan<char> value) => U8Interpolation.AppendFormatted(ref _handler, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendFormatted<T>(T value) => U8Interpolation.AppendFormatted(ref _handler, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendFormatted<T>(T value, ReadOnlySpan<char> format)
            where T : IUtf8SpanFormattable => U8Interpolation.AppendFormatted(ref _handler, value, format);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AppendBytes(ReadOnlySpan<byte> bytes) => U8Interpolation.AppendBytes(ref _handler, bytes);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AppendBytesInlined(ReadOnlySpan<byte> bytes) => U8Interpolation.AppendBytesInlined(ref _handler, bytes);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AppendBytesUnchecked(ReadOnlySpan<byte> bytes) => U8Interpolation.AppendBytesUnchecked(ref _handler, bytes);
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
        builder.Handler.AppendFormatted(value);
        builder.Handler.AppendBytes(U8WriteExtensions.NewLine);
        return builder;
    }

    // TODO: IFormatProvider overload
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static U8Builder Append(
        this U8Builder builder,
        [InterpolatedStringHandlerArgument(nameof(builder))] U8Builder.InterpolatedHandler handler)
    {
        // Roslyn unrolls string interpolation calls into handler ctor with specified args
        // and subsequent AppendLiteral and AppendFormat calls, so the actual appending to
        // builder has already happened by the time we get here.
        return builder;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static U8Builder AppendLine(
        this U8Builder builder,
        [InterpolatedStringHandlerArgument(nameof(builder))] U8Builder.InterpolatedHandler handler)
    {
        builder.Handler.AppendBytes(U8WriteExtensions.NewLine);
        return builder;
    }
}
