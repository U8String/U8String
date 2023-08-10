using System.Collections.Concurrent;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace U8Primitives.Benchmarks;

[MemoryDiagnoser]
[SimpleJob, SimpleJob(RuntimeMoniker.NativeAot80)]
public class Dictionaries
{
    [Params(
        "",
        "Hello",
        "Привіт, Всесвіт!",
        "very long string very long string very long string very long string very long string very long string very long string very long string very long string very long string very long string very long string very long string very long string very long string very long string very long string very long string"
    )]
    public string? StrUtf16;
    public U8String Str;

    private readonly Dictionary<U8String, U8String> Dict = new();
    private readonly ConcurrentDictionary<U8String, U8String> ConcurrentDict = new();
    private readonly Dictionary<string, string?> DictUtf16 = new();
    private readonly ConcurrentDictionary<string, string?> ConcurrentDictUtf16 = new();

    [GlobalSetup]
    public void Setup()
    {
        Str = StrUtf16!.ToU8String();
        Dict[Str.AsSpan().ToU8String()] = Str;
        ConcurrentDict[Str.AsSpan().ToU8String()] = Str;
        DictUtf16[StrUtf16!.AsSpan().ToString()] = StrUtf16;
        ConcurrentDictUtf16[StrUtf16!.AsSpan().ToString()] = StrUtf16;
    }

    [Benchmark(Baseline = true)] public U8String Get() => Dict[Str];
    [Benchmark] public U8String GetConcurrent() => ConcurrentDict[Str];
    [Benchmark] public string? GetUtf16() => DictUtf16[StrUtf16!];
    [Benchmark] public string? GetUtf16Concurrent() => ConcurrentDictUtf16[StrUtf16!];
    [Benchmark] public void Set() => Dict[Str] = Str;
    [Benchmark] public void SetConcurrent() => ConcurrentDict[Str] = Str;
    [Benchmark] public void SetUtf16() => DictUtf16[StrUtf16!] = StrUtf16;
    [Benchmark] public void SetUtf16Concurrent() => ConcurrentDictUtf16[StrUtf16!] = StrUtf16;
}
