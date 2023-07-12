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
    IUtf8SpanFormattable
{
    /// <summary>
    /// Represents an empty <see cref="U8String"/>.
    /// </summary>
    /// <remarks>
    /// Functionally equivalent to <see langword="default(U8String)"/>.
    /// </remarks>
    public static U8String Empty => default;

    // TODO: Store max code point length in Range to short-circuit slicing validation?
    // Or reclaim performance through hand-rolling the validation?
    internal readonly byte[]? Value;
    internal readonly ulong Range;

    internal uint Offset
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (uint)Range;
    }

    internal uint InnerLength
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (uint)(Range >> 32);
    }

    /// <summary>
    /// The number of UTF-8 bytes in the current <see cref="U8String"/>.
    /// </summary>
    /// <returns>The number of UTF-8 bytes.</returns>
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (int)InnerLength;
    }

    /// <summary>
    /// Indicates whether the current <see cref="U8String"/> is empty.
    /// </summary>
    /// <returns><see langword="true"/> if the current <see cref="U8String"/> is empty; otherwise, <see langword="false"/>.</returns>
    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => InnerLength is 0;
    }

    /// <summary>
    /// Must not be accessed if <see cref="IsEmpty"/> is true.
    /// </summary>
    internal ref byte FirstByte
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref System.Runtime.CompilerServices.Unsafe.Add(
            ref MemoryMarshal.GetArrayDataReference(Value!), Offset);
    }

    /// <summary>
    /// Must not be accessed if <see cref="IsEmpty"/> is true.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal byte IndexUnsafe(int index)
    {
        return System.Runtime.CompilerServices.Unsafe.Add(
            ref MemoryMarshal.GetArrayDataReference(Value!), Offset + (uint)index);
    }

    /// <summary>
    /// Must not be accessed if <see cref="IsEmpty"/> is true.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal byte IndexUnsafe(uint index)
    {
        return System.Runtime.CompilerServices.Unsafe.Add(
            ref MemoryMarshal.GetArrayDataReference(Value!), Offset + index);
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
