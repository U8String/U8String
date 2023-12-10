using BenchmarkDotNet.Attributes;

namespace U8.Benchmarks;

[MemoryDiagnoser]
// [DisassemblyDiagnoser(maxDepth: 3, exportCombinedDisassemblyReport: true)]
// [SimpleJob, SimpleJob(RuntimeMoniker.NativeAot80)]
public class ConcatJoin
{
    int[]? Numbers;

    [Params(1, 10, 1000)]
    public int Count;

    [GlobalSetup]
    public void Setup()
    {
        Numbers = Enumerable.Repeat(42, Count).ToArray();
    }

    [Benchmark(Baseline = true)]
    public U8String Concat() => U8String.Concat(Numbers!);

    [Benchmark]
    public string ConcatU16() => string.Concat(Numbers!);

    [Benchmark]
    public U8String Join() => U8String.Join(',', Numbers!);

    [Benchmark]
    public string JoinU16() => string.Join(',', Numbers!);
}
