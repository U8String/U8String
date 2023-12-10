using System.Text;

using BenchmarkDotNet.Attributes;

namespace U8.Benchmarks;

[ShortRunJob]
[DisassemblyDiagnoser(exportCombinedDisassemblyReport: true, maxDepth: 3)]
public class Comparison
{
    public IEnumerable<U8String> Values()
    {
        yield return (U8String)"Hello, World!"u8;
        yield return (U8String)"Привіт, Всесвіт!"u8;
    }

    [ParamsSource(nameof(Values))]
    public U8String Left;

    U8String Right;
    string? Left16;
    string? Right16;

    [GlobalSetup]
    public void Setup()
    {
        Left16 = Left.ToString();
        Right16 = Left16.ToLowerInvariant();
        Right = (U8String)Right16;
    }

    [Benchmark]
    public bool Equals() => Left.Equals(Right);

    [Benchmark]
    public bool EqualsUtf16() => Left16!.Equals(Right16);

    [Benchmark]
    public bool EqualsAsciiIgnoreCase() => Left.Equals(Right, U8Comparison.AsciiIgnoreCase);

    [Benchmark]
    public bool EqualsAsciiIgnoreCaseUtf16() => Ascii.EqualsIgnoreCase(Left16!, Right16!);

    // [Benchmark]
    // public bool EqualsOrdinalIgnoreCase() => Left.Equals(Right, U8Comparison.OrdinalIgnoreCase);

    [Benchmark]
    public bool EqualsOrdinalIgnoreCaseUtf16() => Left16!.Equals(Right16!, StringComparison.OrdinalIgnoreCase);

    [Benchmark]
    public int Compare() => Left.CompareTo(Right);

    [Benchmark]
    public int CompareUtf16() => Left16!.CompareTo(Right16);

    [Benchmark]
    public int CompareAsciiIgnoreCase() => Left.CompareTo(Right, U8Comparison.AsciiIgnoreCase);

    // [Benchmark]
    // public int CompareOrdinalIgnoreCase() => U8String.Compare(Left, Right, U8Comparison.OrdinalIgnoreCase);

    [Benchmark]
    public int CompareOrdinalIgnoreCaseUtf16() => string.Compare(Left16, Right16, StringComparison.OrdinalIgnoreCase);
}
