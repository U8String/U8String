using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using U8Primitives.Unsafe;

namespace U8Primitives.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob, ShortRunJob(RuntimeMoniker.NativeAot80)]
public class Enumeration
{
    private static readonly U8String License = U8Marshal.Create(
        File.ReadAllBytes("../THIRD-PARTY-NOTICES.txt"));

    [Benchmark(Baseline = true)]
    public void Lines()
    {
        foreach (var line in License.Lines)
        {
            _ = line;
        }
    }

    [Benchmark]
    public int LinesBoxedCount() => License.Lines.Count();
}
