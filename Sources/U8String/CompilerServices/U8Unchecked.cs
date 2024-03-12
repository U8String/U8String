using System.Collections.Immutable;
using System.ComponentModel;
using System.Runtime.InteropServices;

using U8.Abstractions;
using U8.InteropServices;
using U8.Primitives;
using U8.Shared;

namespace U8.CompilerServices;

/// <summary>
/// Provides a set of methods for working with UTF-8 strings without verifying if they are valid UTF-8 sequences.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class U8Unchecked
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AppendBytes(ref InlineU8Builder handler, ReadOnlySpan<byte> value)
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
        return new(MemoryMarshal.CreateReadOnlySpanFromNullTerminated(str), skipValidation: true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Join(byte separator, U8String[] values)
    {
        ThrowHelpers.CheckNull(values);
        return U8Manipulation.Join(separator, (ReadOnlySpan<U8String>)values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Join(byte separator, ReadOnlySpan<U8String> values)
    {
        return U8Manipulation.Join(separator, values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Join(byte separator, IEnumerable<U8String> values)
    {
        ThrowHelpers.CheckNull(values);
        return U8Manipulation.Join(separator, values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Join(ReadOnlySpan<byte> separator, U8String[] values)
    {
        ThrowHelpers.CheckNull(values);
        return U8Manipulation.Join(separator, (ReadOnlySpan<U8String>)values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Join(ReadOnlySpan<byte> separator, ReadOnlySpan<U8String> values)
    {
        return U8Manipulation.Join(separator, values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Join(ReadOnlySpan<byte> separator, IEnumerable<U8String> values)
    {
        ThrowHelpers.CheckNull(values);
        return U8Manipulation.Join(separator, values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String StripLineEndings(U8String source)
    {
        return U8Manipulation.StripLineEndings(source);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String LineEndingsToLF(U8String source)
    {
        return U8Manipulation.LineEndingsToLF(source);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String LineEndingsToCRLF(U8String source)
    {
        return U8Manipulation.LineEndingsToCRLF(source);
    }

    public static U8String LineEndingsToCustom(U8String source, byte lineEnding)
    {
        return U8Manipulation.LineEndingsToCustom(source, lineEnding);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String LineEndingsToCustom(U8String source, ReadOnlySpan<byte> lineEnding)
    {
        return U8Manipulation.LineEndingsToCustom(source, lineEnding);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Remove(U8String source, ReadOnlySpan<byte> value)
    {
        return U8Manipulation.Remove(source, value, validate: false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String Replace(U8String source, ReadOnlySpan<byte> oldValue, ReadOnlySpan<byte> newValue)
    {
        return U8Manipulation.Replace(source, oldValue, newValue, validate: false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8RefSplit Split(U8String source, ReadOnlySpan<byte> separator)
    {
        return new U8RefSplit(source, separator);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8RefSplit<T> Split<T>(U8String source, ReadOnlySpan<byte> separator, T comparer)
        where T : IU8Comparer
    {
        return new(source, separator, comparer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8SplitPair SplitFirst(U8String source, ReadOnlySpan<byte> separator)
    {
        return source.SplitFirstUnchecked(separator);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8SplitPair SplitFirst<T>(U8String source, ReadOnlySpan<byte> separator, T comparer)
        where T : IU8IndexOfOperator
    {
        return source.SplitFirstUnchecked(separator, comparer);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8SplitPair SplitLast(U8String source, ReadOnlySpan<byte> separator)
    {
        return source.SplitLastUnchecked(separator);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8SplitPair SplitLast<T>(U8String source, ReadOnlySpan<byte> separator, T comparer)
        where T : IU8LastIndexOfOperator
    {
        return source.SplitLastUnchecked(separator, comparer);
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
