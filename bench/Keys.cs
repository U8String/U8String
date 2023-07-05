using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace U8Primitives.Benchmarks;

[MemoryDiagnoser]
[SimpleJob, SimpleJob(RuntimeMoniker.NativeAot80)]
public class Keys
{
    [Params(
        "fuck",
        "this string is as long as my cock this string is as long as my cock this string is as long as my cock this string is as long as my cock ")]
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