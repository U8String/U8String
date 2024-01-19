using System.Buffers;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

using U8.Shared;

namespace U8.Primitives;

internal struct U8Builder
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
    [ThreadStatic]
    static ArrayBufferWriter<byte>? _cached;
    readonly ArrayBufferWriter<byte> _local;

    public U8Builder()
    {
        _local = Interlocked.Exchange(ref _cached, null)
            ?? new ArrayBufferWriter<byte>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8Builder Append(U8String value)
    {
        if (!value.IsEmpty)
        {
            AppendBytes(value.UnsafeSpan);
        }
        return this;
    }

    public U8Builder Append(ReadOnlySpan<byte> value)
    {
        U8String.Validate(value);
        if (!value.IsEmpty)
        {
            AppendBytes(value);
        }
        return this;
    }

    public U8Builder Append(bool value)
    {
        AppendBytes(value ? "True"u8 : "False"u8);
        return this;
    }

    public U8Builder Append<T>(T value)
    {
        throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8Builder AppendLiteral([ConstantExpected] string value)
    {
        if (value.Length > 0)
        {
            if (value.Length is 1 && char.IsAscii(value[0]))
            {
                AppendByte((byte)value[0]);
            }
            else
            {
                AppendConstantString(value);
            }
        }
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8Builder AppendLiteral(ReadOnlySpan<byte> value)
    {
        U8String.ValidatePossibleConstant(value);
        if (!value.IsEmpty)
        {
            AppendBytes(value);
        }
        return this;
    }

    void AppendByte(byte b)
    {
        // Such overhead per byte is just painful
        _local.GetSpan(1).AsRef() = b;
        _local.Advance(1);
    }

    void AppendBytes(ReadOnlySpan<byte> bytes)
    {
        ref var dst = ref _local.GetSpan(bytes.Length).AsRef();

        bytes.CopyToUnsafe(ref dst);
        _local.Advance(bytes.Length);
    }

    void AppendConstantString([ConstantExpected] string s)
    {
        var literal = U8Literals.Utf16.GetLiteral(s);
        AppendBytes(literal.SliceUnsafe(0, literal.Length - 1));
    }

    public void Dispose()
    {
        _local.ResetWrittenCount();
        _cached = _local;
    }

    [InterpolatedStringHandler]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct InterpolatedHandler
    {
        readonly U8Builder _builder;
        readonly IFormatProvider? _formatProvider;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InterpolatedHandler(
            int literalLength,
            int formattedCount,
            U8Builder builder,
            IFormatProvider? formatProvider = null)
        {
            _builder = builder;
            _formatProvider = formatProvider;

            builder._local.GetSpan(
                literalLength + (formattedCount * 12));
        }

        public void AppendLiteral([ConstantExpected] string s)
        {
            _builder.AppendLiteral(s);
        }

        public void AppendLiteral(ReadOnlySpan<byte> s)
        {
            _builder.AppendLiteral(s);
        }

        public void AppendFormatted<T>(T value)
        {
            _builder.Append(value);
        }
    }
}

[EditorBrowsable(EditorBrowsableState.Never)]
internal static class U8BuilderExtensions
{
    public static U8Builder Append(
        this U8Builder builder,
        [InterpolatedStringHandlerArgument(nameof(builder))] U8Builder.InterpolatedHandler handler)
    {
        // Roslyn unrolls string interpolation calls into handler ctor with specified args
        // and subsequent AppendLiteral and AppendFormat calls, so the actual appending to
        // builder has already happened by the time we get here.
        return builder;
    }
}