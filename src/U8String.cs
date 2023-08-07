using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using U8Primitives.Serialization;

#pragma warning disable IDE1006 // Naming Styles. Why: Exposing internal fields for perf.
namespace U8Primitives;

internal readonly struct U8Range
{
    internal readonly int Offset;
    internal readonly int Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8Range(int offset, int length)
    {
        Debug.Assert((uint)offset <= int.MaxValue);
        Debug.Assert((uint)length <= int.MaxValue);

        Offset = offset;
        Length = length;
    }
}

/// <summary>
/// Represents a UTF-8 encoded string.
/// </summary>
/// <remarks>
/// <para>U8String is an immutable value type that represents a UTF-8 encoded string.</para>
/// <para>It stores UTF-8 code units in the underlying buffer, and provides methods
/// for manipulating and accessing the string content. It can be created from or converted
/// to a string or a span of bytes, as long as the data is valid and convertible to UTF-8.</para>
/// <para>U8String slicing methods are non-copying and return a new U8String that
/// references a portion of the original data. Methods which manipulate the
/// instances of U8String ensure that the resulting U8String is well-formed and valid UTF-8,
/// unless specified otherwise. If an operation would produce invalid UTF-8, an exception is thrown.</para>
/// <para>By default, U8String is indexed by the underlying UTF-8 bytes but offers alternate Rune and Char projections.</para>
/// </remarks>
[DebuggerDisplay("{ToString()}")]
[JsonConverter(typeof(U8StringJsonConverter))]
[CollectionBuilder(typeof(U8String), nameof(Create))]
public readonly partial struct U8String :
    IEquatable<U8String>,
    IEquatable<U8String?>,
    IEquatable<byte[]?>,
    IComparable<U8String>,
    IComparable<U8String?>,
    IComparable<byte[]?>,
    IList<byte>,
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

    internal readonly byte[]? _value;
    internal readonly U8Range _inner;

    internal int Offset
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _inner.Offset;
    }

    /// <summary>
    /// The number of UTF-8 bytes in the current <see cref="U8String"/>.
    /// </summary>
    /// <returns>The number of UTF-8 bytes.</returns>
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _inner.Length;
    }

    /// <summary>
    /// Indicates whether the current <see cref="U8String"/> is empty.
    /// </summary>
    /// <returns><see langword="true"/> if the current <see cref="U8String"/> is empty; otherwise, <see langword="false"/>.</returns>
    [MemberNotNullWhen(false, nameof(_value))]
    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _value is null;
    }

    /// <inheritdoc/>
    int ICollection<byte>.Count => Length;

    /// <inheritdoc/>
    bool ICollection<byte>.IsReadOnly => true;

    /// <summary>
    /// Must not be accessed if <see cref="IsEmpty"/> is true.
    /// </summary>
    internal ref byte UnsafeRef
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Unsafe.Add(
            ref MemoryMarshal.GetArrayDataReference(_value!), (nint)(uint)Offset);
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
            ref MemoryMarshal.GetArrayDataReference(_value!), (nint)(uint)Offset + (nint)(uint)index);
    }

    /// <summary>
    /// Must not be accessed if <see cref="IsEmpty"/> is true.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref byte UnsafeRefAdd(uint index)
    {
        return ref Unsafe.Add(
            ref MemoryMarshal.GetArrayDataReference(_value!), (nint)(uint)Offset + (nint)index);
    }

    /// <summary>
    /// Evaluates if the current <see cref="U8String"/> contains only ASCII characters.
    /// </summary>
    public bool IsAscii() => Ascii.IsValid(this);

    /// <summary>
    /// Evaluates if the current <see cref="U8String"/> is normalized to the specified
    /// Unicode normalization form (default: <see cref="NormalizationForm.FormC"/>).
    /// </summary>
    public bool IsNormalized(NormalizationForm form = NormalizationForm.FormC) => throw new NotImplementedException();

    /// <summary>
    /// Validates that the <paramref name="value"/> is a valid UTF-8 byte sequence.
    /// </summary>
    /// <param name="value">The <see cref="ReadOnlySpan{T}"/> to validate.</param>
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool ValidateSlice(ReadOnlySpan<byte> value, int offset, int length)
    {
        // TODO: Another method which requires like 10 iterations to achieve good codegen.
        throw new NotImplementedException();
    }

    void IList<byte>.Insert(int index, byte item) => throw new NotImplementedException();
    void IList<byte>.RemoveAt(int index) => throw new NotImplementedException();
    void ICollection<byte>.Add(byte item) => throw new NotImplementedException();
    void ICollection<byte>.Clear() => throw new NotImplementedException();
    bool ICollection<byte>.Remove(byte item) => throw new NotImplementedException();
}
