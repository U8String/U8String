using System.Collections.Immutable;

namespace U8Primitives.Abstractions;

public interface IU8ContainsOperator
{
    bool Contains(ReadOnlySpan<byte> source, byte value);
    bool Contains(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value);
}

public interface IU8IndexOfOperator
{
    int IndexOf(ReadOnlySpan<byte> source, byte value);
    int IndexOf(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value);
}

public interface IU8EqualityComparer : IEqualityComparer<U8String>
{
    bool Equals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right);

    int GetHashCode(ReadOnlySpan<byte> obj);
}

// TODO: Member naming?
public interface IU8CaseConverter
{
    (int Offset, int ResultLength) UppercaseHint(ReadOnlySpan<byte> source);
    (int Offset, int ResultLength) LowercaseHint(ReadOnlySpan<byte> source);
    int ToLower(ReadOnlySpan<byte> source, Span<byte> destination);
    int ToUpper(ReadOnlySpan<byte> source, Span<byte> destination);
}

public interface IEnumerable<T, TEnumerator> : IEnumerable<T>
    where TEnumerator : struct, IEnumerator<T>
{
    new TEnumerator GetEnumerator();
}

public interface IU8Enumerable<TEnumerator> : IEnumerable<U8String, TEnumerator>
    where TEnumerator : struct, IU8Enumerator
{ }

public interface IU8Enumerator : IEnumerator<U8String> { }

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
