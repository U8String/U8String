using BenchmarkDotNet.Attributes;

namespace U8Primitives.Benchmarks;

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
        "\t\n\v\f\r 👋🏻🌍\t\n\v\f\r "
    )]
    public string ValueU16 = string.Empty;

    public U8String Value;

    [GlobalSetup]
    public void Setup() => Value = new U8String(ValueU16);

    [Benchmark(Baseline = true)]
    public U8String Trim() => Value.Trim();

    [Benchmark]
    public string TrimU16() => ValueU16.Trim();
}
