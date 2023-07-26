using BenchmarkDotNet.Attributes;

namespace U8Primitives.Benchmarks;

[SimpleJob]
[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 3, exportCombinedDisassemblyReport: true)]
public class HashSet
{
    [Params(
        "test",
        "тест",
        "very long string very long string very long string very long string very long string very long string very long string very long string very long string very long string very long string very long string very long string very long string very long string very long string very long string very long string"
    )]
    public string? Value;
    public U8String ValueU8;

    private readonly HashSet<string> Strings = new();
    private readonly HashSet<U8String> U8Strings = new();

    [GlobalSetup]
    public void Setup()
    {
        ValueU8 = Value!.ToU8String();
        Strings.Add(Value!.AsSpan().ToString());
        U8Strings.Add(ValueU8.AsSpan().ToU8String());
    }

    [Benchmark(Baseline = true)]
    public bool GetString()
    {
        return Strings.Contains(Value!);
    }

    [Benchmark]
    public bool GetU8String()
    {
        return U8Strings.Contains(ValueU8);
    }

    [Benchmark]
    public void SetString()
    {
        Strings.Add(Value!);
    }

    [Benchmark]
    public void SetU8String()
    {
        U8Strings.Add(ValueU8);
    }
}
