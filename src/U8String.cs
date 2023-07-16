using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using U8Primitives.Serialization;

namespace U8Primitives;

#pragma warning disable IDE1006 // Naming Styles. Why: Exposing internal fields for perf.
/// <summary>
/// Represents a UTF-8 encoded string.
/// </summary>
/// <remarks>
/// <para>U8String is an immutable value type that represents a UTF-8 encoded string.</para>
/// <para>It stores the UTF-8 code units in an underlying byte[] buffer, and provides methods
/// for manipulating and accessing the string content. It can be created from or converted
/// to a string or a span of bytes, as long as the data is valid or convertible to UTF-8.</para>
/// <para>U8String provides non-copying substringing and slicing operations, which return a new
/// U8String that references a portion of the original data. Methods which manipulate the
/// instances of U8String ensure that the resulting U8String is well-formed and valid UTF-8,
/// unless otherwise specified. If an operation would produce invalid UTF-8, an exception is thrown.</para>
/// <para>By default, U8String is indexed by the underlying UTF-8 bytes but offers alternate Rune and Char projections.</para>
/// </remarks>
[JsonConverter(typeof(U8StringJsonConverter))]
public readonly partial struct U8String :
    IEquatable<U8String>,
    IEquatable<U8String?>,
    IEquatable<byte[]>,
    ICloneable,
    ISpanParsable<U8String>,
    ISpanFormattable,
    IUtf8SpanParsable<U8String>,
    IUtf8SpanFormattable
{
    /// <summary>
    /// Represents an empty <see cref="U8String"/>.
    /// </summary>
    /// <remarks>
    /// Functionally equivalent to <see langword="default(U8String)"/>.
    /// </remarks>
    public static U8String Empty => default;

    internal readonly byte[]? Value;

    private readonly InnerOffsets Inner;

    [StructLayout(LayoutKind.Sequential)]
    readonly struct InnerOffsets
    {
        public readonly int Offset;
        public readonly int Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InnerOffsets(int offset, int length)
        {
            Debug.Assert((uint)offset <= int.MaxValue);
            Debug.Assert((uint)length <= int.MaxValue);

            Offset = offset;
            Length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ulong(InnerOffsets value)
        {
            var inner = value;
            return Unsafe.As<InnerOffsets, ulong>(ref inner);
        }
    }

    internal int Offset
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            // !! Tracking issue https://github.com/dotnet/runtime/issues/88950 !!
            // var inner = Inner;
            // return Unsafe.As<ulong, InnerOffsets>(ref inner).Offset;
            return Inner.Offset;
        }
    }

    /// <summary>
    /// The number of UTF-8 bytes in the current <see cref="U8String"/>.
    /// </summary>
    /// <returns>The number of UTF-8 bytes.</returns>
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            // !! Tracking issue https://github.com/dotnet/runtime/issues/88950 !!
            // var inner = Inner;
            // return Unsafe.As<ulong, InnerOffsets>(ref inner).Length;
            return Inner.Length;
        }
    }

    /// <summary>
    /// Indicates whether the current <see cref="U8String"/> is empty.
    /// </summary>
    /// <returns><see langword="true"/> if the current <see cref="U8String"/> is empty; otherwise, <see langword="false"/>.</returns>
    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Value is null;
    }

    /// <summary>
    /// Must not be accessed if <see cref="IsEmpty"/> is true.
    /// </summary>
    internal ref byte UnsafeRef
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Unsafe.Add(
            ref MemoryMarshal.GetArrayDataReference(Value!), (nint)(uint)Offset);
    }

    internal ReadOnlySpan<byte> UnsafeSpan
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => MemoryMarshal.CreateReadOnlySpan(ref UnsafeRef, Length);
    }

    /// <summary>
    /// Must not be accessed if <see cref="IsEmpty"/> is true.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref byte UnsafeRefAdd(int index)
    {
        return ref Unsafe.Add(
            ref MemoryMarshal.GetArrayDataReference(Value!), (nint)(uint)Offset + (nint)(uint)index);
    }

    /// <summary>
    /// Must not be accessed if <see cref="IsEmpty"/> is true.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref byte UnsafeRefAdd(uint index)
    {
        return ref Unsafe.Add(
            ref MemoryMarshal.GetArrayDataReference(Value!), (nint)(uint)Offset + (nint)index);
    }

    /// <summary>
    /// Evaluates whether the current <see cref="U8String"/> contains only ASCII characters.
    /// </summary>
    public bool IsAscii() => Ascii.IsValid(this);

    /// <summary>
    /// Validates that the <paramref name="value"/> is a valid UTF-8 byte sequence.
    /// </summary>
    /// <param name="value">The <see cref="ReadOnlySpan{T}"/> to validate.</param>
    /// <returns>
    /// <see langword="true"/> if the <paramref name="value"/> contains a valid UTF-8 byte sequence; otherwise, <see langword="false"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid(ReadOnlySpan<byte> value) => Utf8.IsValid(value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Validate(ReadOnlySpan<byte> value)
    {
        if (!IsValid(value))
        {
            ThrowHelpers.InvalidUtf8();
        }
    }
}
