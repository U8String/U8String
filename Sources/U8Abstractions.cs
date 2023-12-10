namespace U8.Abstractions;

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

public interface IU8StartsWithOperator
{
    bool StartsWith(ReadOnlySpan<byte> source, byte value);
    bool StartsWith(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value);
}

public interface IU8EndsWithOperator
{
    bool EndsWith(ReadOnlySpan<byte> source, byte value);
    bool EndsWith(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value);
}

public interface IU8Comparer : IComparer<U8String>
{
    int Compare(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y);
}

public interface IU8EqualityComparer : IEqualityComparer<U8String>
{
    bool Equals(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y);
    int GetHashCode(ReadOnlySpan<byte> value);
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
    where TEnumerator : struct, IU8Enumerator;

public interface IU8Enumerator : IEnumerator<U8String>;
