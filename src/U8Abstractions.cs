using System.Collections.Immutable;

namespace U8Primitives.Abstractions;

// Decision: This waits post 1.0.0 release to better understand the practical use cases
// and desired API shape to accomodate possible user defined implementations.
// In preparation for supporing NativeU8String, MutableU8String, etc.?
// TODO: Decide on API shape for this or if it's even needed. Maybe for the version 2?
// It is always an option to implement the interface on a type later on.
// TODO 2: Consider comparer permutations to balance between boilerplate
// and user experience of being able to compare different implementations.
internal interface IU8String<T> :
    IList<byte>,
    IEquatable<T>,
    IComparable<T>,
    IUtf8SpanFormattable
        where T : IU8String<T>
{
    static abstract T Create(ReadOnlySpan<byte> value);
    static abstract T Create(ReadOnlySpan<char> value);
    static abstract T Create(ImmutableArray<byte> value);
    static abstract T CreateUnchecked(ReadOnlySpan<byte> value);
    static abstract T CreateUnchecked(ImmutableArray<byte> value);

    ReadOnlySpan<byte> AsSpan();
    ReadOnlySpan<byte> AsSpan(int start);
    ReadOnlySpan<byte> AsSpan(int start, int length);

    T Slice(int start);
    T Slice(int start, int length);
}

internal interface IU8Enumerable<TEnumerator> : IEnumerable<U8String>
    where TEnumerator : IU8Enumerator
{ }

internal interface IU8Enumerator : IEnumerator<U8String> { }