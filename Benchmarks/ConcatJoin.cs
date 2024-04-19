using BenchmarkDotNet.Attributes;

namespace U8.Benchmarks;

[MemoryDiagnoser]
// [DisassemblyDiagnoser(maxDepth: 3, exportCombinedDisassemblyReport: true)]
// [SimpleJob, SimpleJob(RuntimeMoniker.NativeAot80)]
public class ConcatJoin
{
    U8String[]? Numbers;

    string[]? NumbersU16;

    [Params(1, 10, 1000)]
    public int Count;

    [GlobalSetup]
    public void Setup()
    {
        Numbers = Enumerable.Repeat(42, Count).Select(u8).ToArray();
        NumbersU16 = Numbers.Select(x => x.ToString()).ToArray();
    }

    [Benchmark(Baseline = true)]
    public U8String Concat() => U8String.Concat(Numbers!);

    [Benchmark]
    public string ConcatU16() => string.Concat(NumbersU16!);

    [Benchmark]
    public U8String Join() => U8String.Join(',', Numbers!);

    [Benchmark]
    public string JoinU16() => string.Join(',', NumbersU16!);
}
