using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Unicode;

using U8Primitives.Serialization;

#pragma warning disable IDE1006 // Naming Styles. Why: Exposing internal fields for perf.
namespace U8Primitives;

/// <summary>
/// UTF-8 encoded string.
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
    public static U8String Empty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => default;
    }

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

    public bool IsNullTerminated
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            var (value, offset, length) = this;

            if (value != null)
            {
                var end = offset + length;
                // Explicitly split AsRef and Add to skip Debug assert.
                // This is intended since ptr is potentially out of bounds.
                ref var ptr = ref value.AsRef().Add(end);

                return ((uint)end < (uint)value.Length && ptr is 0) || ptr.Subtract(1) is 0;
            }

            return false;
        }
    }

    public U8Range Range
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _inner;
    }

    /// <summary>
    /// The number of UTF-8 code points in the current <see cref="U8String"/>.
    /// </summary>
    /// <remarks>
    /// Evaluation of this property becomes O(n) above 50-100 bytes.
    /// </remarks>
    public int RuneCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (!IsEmpty)
            {
                return U8Searching.CountRunes(ref UnsafeRef, (uint)Length);
            }

            return 0;
        }
    }

    public U8Source Source
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => new(_value);
    }

    /// <inheritdoc/>
    int ICollection<byte>.Count => Length;

    /// <inheritdoc/>
    bool ICollection<byte>.IsReadOnly => true;

    /// <summary>
    /// Similar to <see cref="UnsafeRef"/>, but does not throw NRE if <see cref="IsEmpty"/> is true.
    /// </summary>
    /// <remarks>
    /// cmov's the ref out of byte[] if it is not null and uncoditionally increments it by <see cref="Offset"/>.
    /// </remarks>
    internal ref byte DangerousRef
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            ref var reference = ref Unsafe.NullRef<byte>();
            var (value, offset) = (_value, (nint)(uint)Offset);
            if (value != null)
            {
                reference = ref Unsafe.Add(
                    ref MemoryMarshal.GetArrayDataReference(value), offset);
            }

            return ref reference;
        }
    }

    /// <summary>
    /// Will throw NRE if <see cref="IsEmpty"/> is true.
    /// </summary>
    internal ref byte UnsafeRef
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Unsafe.Add(
            ref MemoryMarshal.GetArrayDataReference(_value!), (nint)(uint)Offset);
    }

    /// <summary>
    /// Will throw NRE if <see cref="IsEmpty"/> is true.
    /// </summary>
    internal ReadOnlySpan<byte> UnsafeSpan
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => MemoryMarshal.CreateReadOnlySpan(ref UnsafeRef, Length);
    }

    /// <summary>
    /// Will throw NRE if <see cref="IsEmpty"/> is true.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref byte UnsafeRefAdd(int index)
    {
        return ref Unsafe.Add(
            ref MemoryMarshal.GetArrayDataReference(_value!), (nint)(uint)Offset + (nint)(uint)index);
    }

    /// <summary>
    /// Evaluates if the current <see cref="U8String"/> contains only ASCII characters.
    /// </summary>
    public bool IsAscii() => System.Text.Ascii.IsValid(this);

    /// <summary>
    /// Evaluates if the current <see cref="U8String"/> is normalized to the specified
    /// Unicode normalization form (default: <see cref="NormalizationForm.FormC"/>).
    /// </summary>
    internal bool IsNormalized(NormalizationForm form = NormalizationForm.FormC)
    {
        var source = this;
        if (!source.IsEmpty)
        {
            var value = source.UnsafeSpan;

            // Drain always normalized ASCII bytes
            var nonAsciiOffset = (int)Polyfills.Text.Ascii.GetIndexOfFirstNonAsciiByte(value);
            value = value.SliceUnsafe(nonAsciiOffset);

            if (value.Length > 0)
            {
                throw new NotImplementedException();
            }
        }

        return true;
    }

    /// <summary>
    /// Validates that the <paramref name="value"/> is a valid UTF-8 byte sequence.
    /// </summary>
    /// <param name="value">The <see cref="ReadOnlySpan{T}"/> to validate.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValid(ReadOnlySpan<byte> value)
    {
        if (value.Length > 1)
        {
            return Utf8.IsValid(value);
        }
        else if (value.Length > 0)
        {
            return U8Info.IsAsciiByte(in value.AsRef());
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Validate(ReadOnlySpan<byte> value)
    {
        if (!IsValid(value))
        {
            ThrowHelpers.InvalidUtf8();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void Deconstruct(out byte[]? value, out int offset, out int length)
    {
        value = _value;
        offset = Offset;
        length = Length;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref readonly byte GetPinnableReference() => ref DangerousRef;

    void IList<byte>.Insert(int index, byte item) => throw new NotSupportedException();
    void IList<byte>.RemoveAt(int index) => throw new NotSupportedException();
    void ICollection<byte>.Add(byte item) => throw new NotSupportedException();
    void ICollection<byte>.Clear() => throw new NotSupportedException();
    bool ICollection<byte>.Remove(byte item) => throw new NotSupportedException();
}
