using BenchmarkDotNet.Attributes;

namespace U8Primitives.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
// [DisassemblyDiagnoser(maxDepth: 3, exportCombinedDisassemblyReport: true)]
public class Dictionary
{
    [Params(
        "test",
        "тест",
        "very long string very long string very long string very long string very long string very long string very long string very long string very long string very long string very long string very long string very long string very long string very long string very long string very long string very long string"
    )]
    public string? Value;
    public U8String ValueU8;

    private readonly Dictionary<string, string?> Strings = new();
    private readonly Dictionary<U8String, U8String> U8Strings = new();

    [GlobalSetup]
    public void Setup()
    {
        ValueU8 = Value!.ToU8String();
        Strings[Value!.AsSpan().ToString()] = Value;
        U8Strings[ValueU8.AsSpan().ToU8String()] = ValueU8;
    }

    [Benchmark(Baseline = true)]
    public string? GetString()
    {
        return Strings[Value!];
    }

    [Benchmark]
    public U8String GetU8String()
    {
        return U8Strings[ValueU8];
    }

    [Benchmark]
    public void SetString()
    {
        Strings[Value!] = Value;
    }

    [Benchmark]
    public void SetU8String()
    {
        U8Strings[ValueU8] = ValueU8;
    }
}
