using System.Collections.Concurrent;
using System.Collections.Frozen;

using BenchmarkDotNet.Attributes;

namespace U8.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
[DisassemblyDiagnoser(maxDepth: 3, exportCombinedDisassemblyReport: true)]
// [ShortRunJob, ShortRunJob(RuntimeMoniker.NativeAot80)]
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

    private readonly Dictionary<U8String, U8String> Dict = [];
    private FrozenDictionary<U8String, U8String> FrozenDict = null!;
    private readonly ConcurrentDictionary<U8String, U8String> ConcurrentDict = new();
    private readonly Dictionary<string, string?> DictUtf16 = [];
    private FrozenDictionary<string, string?> FrozenDictUtf16 = null!;
    private readonly ConcurrentDictionary<string, string?> ConcurrentDictUtf16 = new();

    [GlobalSetup]
    public void Setup()
    {
        var firstU8 = new U8String(StrUtf16!);
        var secondU8 = firstU8.Clone();
        var secondU16 = firstU8.ToString();

        Str = firstU8;
        Dict[secondU8] = secondU8;
        DictUtf16[secondU16] = secondU16;
        FrozenDict = Dict.ToFrozenDictionary();
        FrozenDictUtf16 = DictUtf16.ToFrozenDictionary();
        ConcurrentDict[secondU8] = secondU8;
        ConcurrentDictUtf16[secondU16] = secondU16;
    }

    [Benchmark(Baseline = true)] public U8String Get() => Dict[Str];
    [Benchmark] public string? GetUtf16() => DictUtf16[StrUtf16!];
    [Benchmark] public U8String GetFrozen() => FrozenDict[Str];
    [Benchmark] public string? GetUtf16Frozen() => FrozenDictUtf16[StrUtf16!];
    [Benchmark] public U8String GetConcurrent() => ConcurrentDict[Str];
    [Benchmark] public string? GetUtf16Concurrent() => ConcurrentDictUtf16[StrUtf16!];
    [Benchmark] public void Set() => Dict[Str] = Str;
    [Benchmark] public void SetUtf16() => DictUtf16[StrUtf16!] = StrUtf16;
    [Benchmark] public void SetConcurrent() => ConcurrentDict[Str] = Str;
    [Benchmark] public void SetUtf16Concurrent() => ConcurrentDictUtf16[StrUtf16!] = StrUtf16;
}
