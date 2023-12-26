using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace U8.Benchmarks;

[ShortRunJob, ShortRunJob(RuntimeMoniker.NativeAot80)]
//[DisassemblyDiagnoser(maxDepth: 2, exportCombinedDisassemblyReport: true)]
public class Slicing
{
    readonly U8String Value = (U8String)"Hello, World!"u8;
    readonly string Value16 = "Hello, World!";

    readonly int Start = 3;
    readonly int End = 8;

    [Benchmark]
    public U8String Slice() => Value[Start..End];

    [Benchmark]
    public U8String SliceRounding() => Value.SliceRounding(Start, End);

    [Benchmark]
    public string Slice16() => Value16[Start..End];

    [Benchmark]
    public U8String SliceStart() => Value[Start..];

    [Benchmark]
    public U8String SliceRoundingStart()
    {
        var start = Start;
        var value = Value;
        return value.SliceRounding(start, value.Length - start);
    }

    [Benchmark]
    public string Slice16Start() => Value16[Start..];

    [Benchmark]
    public U8String SliceEnd() => Value[..End];

    [Benchmark]
    public U8String SliceRoundingEnd() => Value.SliceRounding(0, End);

    [Benchmark]
    public string Slice16End() => Value16[..End];

    [Benchmark]
    public U8String SliceConsts() => Value[3..8];

    [Benchmark]
    public string Slice16Consts() => Value16[3..8];
}
