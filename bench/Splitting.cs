using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace U8Primitives.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
// [ShortRunJob, ShortRunJob(RuntimeMoniker.NativeAot80)]
// [DisassemblyDiagnoser(maxDepth: 3, exportCombinedDisassemblyReport: true)]
public class Splitting
{
    [Params(
        "test",
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed non risus. Suspendisse",
        "Привіт, Всесвіт!"
    )]
    public string? ValueUtf16 { get; set; }
    public U8String Value { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Value = new U8String(ValueUtf16);
    }

    [Benchmark]
    public int SplitCount() => Value.Split(',').Count;

    [Benchmark]
    public int SplitSeqCount() => Value.Split(", "u8).Count;

    [Benchmark]
    public U8String SplitFirst() => Value.SplitFirst(',').Segment;

    [Benchmark]
    public string SplitFirstUtf16() => ValueUtf16!.Split(',')[0];

    [Benchmark]
    public U8String SplitFirstSeq() => Value.SplitFirst(", "u8).Segment;

    [Benchmark]
    public string SplitFirstSeqUtf16() => ValueUtf16!.Split(", ")[0];
}
