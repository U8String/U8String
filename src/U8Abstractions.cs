using System.Collections.Immutable;

namespace U8Primitives.Abstractions;

#pragma warning disable IDE0057 // Use range operator. Why: Performance.
public interface IU8ContainsOperator
{
    bool Contains(ReadOnlySpan<byte> source, byte value);
    bool Contains(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value);
}

public interface IU8CountOperator
{
    int Count(ReadOnlySpan<byte> source, byte value);
    int Count(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value);
}

// Note: it is expected for these to be called by U8Searching and similar, so that
// interface implementations don't have to double-check for length is 1 -> fast path.
// Flow: public method -> (enumerator ->) U8Searching/Impls -> interface implementation
public interface IU8IndexOfOperator
{
    (int Offset, int Length) IndexOf(ReadOnlySpan<byte> source, byte value);
    (int Offset, int Length) IndexOf(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value);
}

public interface IU8LastIndexOfOperator
{
    (int Offset, int Length) LastIndexOf(ReadOnlySpan<byte> source, byte value);
    (int Offset, int Length) LastIndexOf(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value);
}

public interface IU8EqualityComparer : IEqualityComparer<U8String>
{
    bool Equals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right);
    int GetHashCode(ReadOnlySpan<byte> obj);
}

// TODO: Member naming?
public interface IU8CaseConverter
{
    // {Lower/Upper}Length indicates the *total* length of the source if it were to be converted.
    // ReplaceStart indicates the start of the first character that would be replaced, if any.
    // If ReplaceStart is -1, then no characters would be replaced.
    (int ReplaceStart, int LowercaseLength) LowercaseHint(ReadOnlySpan<byte> source);
    (int ReplaceStart, int UppercaseLength) UppercaseHint(ReadOnlySpan<byte> source);
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
