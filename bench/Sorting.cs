using BenchmarkDotNet.Attributes;

namespace U8Primitives.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 4, exportCombinedDisassemblyReport: true)]
public class Sorting
{
    static readonly string[] Strings = new[]
    {
        "test",
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed non risus. Suspendisse",
        "Привіт, Всесвіт!",
        "hello",
        "goodbye",
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed non risus. Suspendisse potenti. Maecenas feugiat"
    };

    static readonly U8String[] U8Strings = new[]
    {
        new U8String(Strings[0]),
        new U8String(Strings[1]),
        new U8String(Strings[2]),
        new U8String(Strings[3]),
        new U8String(Strings[4]),
        new U8String(Strings[5])
    };

    [Benchmark]
    public void Sort()
    {
        Array.Sort(U8Strings);
    }

    [Benchmark]
    public void SortUtf16()
    {
        Array.Sort(Strings);
    }

    [Benchmark]
    public void SortSpecialized()
    {
        Array.Sort(U8Strings, U8Comparer.Ordinal);
    }

    [Benchmark]
    public void SortSpecializedUtf16()
    {
        Array.Sort(Strings, StringComparer.Ordinal);
    }

    [Benchmark]
    public U8String[] SortCollect() => U8Strings.Order().ToArray();

    [Benchmark]
    public string[] SortCollectUtf16() => Strings.Order().ToArray();
}
