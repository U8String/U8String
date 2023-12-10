using BenchmarkDotNet.Attributes;

namespace U8.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[DisassemblyDiagnoser(exportCombinedDisassemblyReport: true, maxDepth: 3)]
public class Manipulation
{
    [Params(
        "",
        "Hello",
        "Привіт",
        "Hello ",
        "     ",
        "\t\n\v\f\r 👋🏻🌍\t\n\v\f\r "
    )]
    public string ValueU16 = string.Empty;

    public U8String Value;

    U8String[]? Values;

    string[]? ValuesU16;

    [GlobalSetup]
    public void Setup()
    {
        Value = new U8String(ValueU16);
        Values = Enumerable.Repeat(Value, 10).ToArray();
        ValuesU16 = Enumerable.Repeat(ValueU16, 10).ToArray();
    }

    [Benchmark(Baseline = true)]
    public U8String Trim() => Value.Trim();

    [Benchmark]
    public string TrimU16() => ValueU16.Trim();

    [Benchmark]
    public U8String Concat() => Value + Value;

    [Benchmark]
    public string ConcatU16() => ValueU16 + ValueU16;

    [Benchmark]
    public U8String ConcatMany() => U8String.Concat(Values!);

    [Benchmark]
    public string ConcatManyU16() => string.Concat(ValuesU16!);

    [Benchmark]
    public U8String Join() => U8String.Join(", "u8, Values!);

    [Benchmark]
    public string JoinU16() => string.Join(", ", ValuesU16!);
}
