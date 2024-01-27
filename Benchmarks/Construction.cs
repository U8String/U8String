using System.Text;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace U8.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob, ShortRunJob(RuntimeMoniker.NativeAot80)]
// [DisassemblyDiagnoser(maxDepth: 3, exportCombinedDisassemblyReport: true)]
public class Construction
{
    [Params(0, 5, 10, 100, 1000, 10_000)]
    public int Numbers { get; set; }

    private byte[]? Bytes;
    private string? Str;

    [GlobalSetup]
    public void Setup()
    {
        Str = (0..Numbers)
            .Aggregate(new StringBuilder(), (sb, i) => sb.Append(i))
            .ToString();

        Bytes = Encoding.UTF8.GetBytes(Str);
    }

    [Benchmark]
    public U8String FromBytes() => u8(Bytes!);

    [Benchmark]
    public U8String FromString() => u8(Str!);
}
