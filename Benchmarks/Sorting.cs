using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace U8.Benchmarks;

[SimpleJob, SimpleJob(RuntimeMoniker.NativeAot80)]
[MemoryDiagnoser]
// [DisassemblyDiagnoser(maxDepth: 4, exportCombinedDisassemblyReport: true)]
public class Sorting
{
    public int Length = 1_000_000;

    U8String[] Strings = null!;
    string[] Utf16Strings = null!;

    [GlobalSetup]
    public void Setup()
    {
        Strings = (0..Length).Select(i => i.ToU8String()).ToArray();
        Utf16Strings = (0..Length).Select(i => $"{i}").ToArray();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        Random.Shared.Shuffle(Strings);
        Random.Shared.Shuffle(Utf16Strings);
    }

    [Benchmark]
    public void Sort()
    {
        Array.Sort(Strings);
    }

    [Benchmark]
    public void SortUtf16()
    {
        Array.Sort(Utf16Strings);
    }

    [Benchmark]
    public void SortSpecialized()
    {
        Array.Sort(Strings, U8Comparison.Ordinal);
    }

    [Benchmark]
    public void SortSpecializedUtf16()
    {
        Array.Sort(Utf16Strings, StringComparer.Ordinal);
    }

    [Benchmark]
    public U8String[] SortCollect() => Strings.Order().ToArray();

    [Benchmark]
    public string[] SortCollectUtf16() => Utf16Strings.Order().ToArray();
}
