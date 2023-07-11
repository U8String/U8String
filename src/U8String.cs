using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using U8Primitives.Serialization;

namespace U8Primitives;

#pragma warning disable CA1825 // Avoid zero-length array allocations. Why: cctor checks ruin codegen
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
    public static U8String Empty => default;

    internal readonly byte[]? _value;

    internal readonly uint _offset;

    internal readonly uint _length;

    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (int)_length;
    }

    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _length is 0;
    }

    /// <summary>
    /// Must not be accessed if <see cref="IsEmpty"/> is true.
    /// </summary>
    internal ref byte FirstByte
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref System.Runtime.CompilerServices.Unsafe.Add(
            ref MemoryMarshal.GetArrayDataReference(_value!), _offset);
    }

    /// <summary>
    /// Must not be accessed if <see cref="IsEmpty"/> is true.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal byte IndexUnsafe(int index)
    {
        return System.Runtime.CompilerServices.Unsafe.Add(
            ref MemoryMarshal.GetArrayDataReference(_value!), _offset + (uint)index);
    }

    /// <summary>
    /// Must not be accessed if <see cref="IsEmpty"/> is true.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal byte IndexUnsafe(uint index)
    {
        return System.Runtime.CompilerServices.Unsafe.Add(
            ref MemoryMarshal.GetArrayDataReference(_value!), _offset + index);
    }

    public bool IsAscii() => Ascii.IsValid(this);

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
