using System.Collections.Immutable;
using System.ComponentModel;
using System.Runtime.InteropServices;

using U8.Primitives;

namespace U8.InteropServices;

/// <summary>
/// Provides a set of methods for working with UTF-8 strings without verifying if they are valid UTF-8 sequences.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Advanced)]
public static class U8Unchecked
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AppendBytes(ref InterpolatedU8StringHandler handler, ReadOnlySpan<byte> value)
    {
        handler.AppendBytes(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AppendBytes(U8Builder builder, ReadOnlySpan<byte> value)
    {
        builder.AppendBytes(value);
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from <paramref name="value"/> without verifying
    /// if it is a valid UTF-8 sequence.
    /// </summary>
    /// <param name="value">The UTF-8 bytes to create the <see cref="U8String"/> from.</param>
    /// <remarks>
    /// The <see cref="U8String"/> will be created by copying the <paramref name="value"/> bytes if the length is greater than 0.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Create(ReadOnlySpan<byte> value)
    {
        return new(value, skipValidation: true);
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from <paramref name="value"/> without verifying
    /// if it is a valid UTF-8 sequence.
    /// </summary>
    /// <param name="value">The UTF-8 bytes to create the <see cref="U8String"/> from.</param>
    /// <remarks>
    /// <para>
    /// The <see cref="U8String"/> will be created by taking the underlying reference from the
    /// <paramref name="value"/> without copying if the length is greater than 0.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Create(ImmutableArray<byte> value)
    {
        var bytes = ImmutableCollectionsMarshal.AsArray(value);
        if (bytes != null)
        {
            return new(bytes, 0, bytes.Length);
        }

        return default;
    }

    /// <summary>
    /// Creates a new <see cref="U8String"/> from null-terminated <paramref name="str"/> without verifying
    /// if it is a valid UTF-8 sequence. This is an unchecked variant of <see cref="U8String.Create(byte*)"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe U8String Create(byte* str)
    {
        var length = U8Marshal.IndexOfNullByte(str);
        if (length > (nuint)Array.MaxLength - 1)
        {
            ThrowHelpers.DestinationTooShort();
        }

        if (length > 0)
        {
            var bytes = new byte[length + 1];
            MemoryMarshal
                .CreateSpan(ref Unsafe.AsRef<byte>(str), (int)(uint)length)
                .CopyToUnsafe(ref bytes.AsRef());

            return new(bytes, 0, (int)(uint)length);
        }

        return default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8SplitPair SplitFirst(U8String source, ReadOnlySpan<byte> separator)
    {
        return source.SplitFirstUnchecked(separator);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8SplitPair SplitLast(U8String source, ReadOnlySpan<byte> separator)
    {
        return source.SplitLastUnchecked(separator);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Strip(U8String source, ReadOnlySpan<byte> value)
    {
        return source.StripUnchecked(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Strip(U8String source, ReadOnlySpan<byte> prefix, ReadOnlySpan<byte> suffix)
    {
        return source.StripUnchecked(prefix, suffix);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String StripPrefix(U8String source, ReadOnlySpan<byte> value)
    {
        return source.StripPrefixUnchecked(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String StripSuffix(U8String source, ReadOnlySpan<byte> value)
    {
        return source.StripSuffixUnchecked(value);
    }
}
