using BenchmarkDotNet.Attributes;

namespace U8Primitives.Benchmarks;

[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 3, exportCombinedDisassemblyReport: true)]
// [ShortRunJob, ShortRunJob(RuntimeMoniker.NativeAot80)]
public class Enumeration
{
    [Params(100, 1000, 10000)]
    public int Length;

    U8String ThirdPartyNotices;

    string? ThirdPartyNoticesU16;

    [GlobalSetup]
    public void Setup()
    {
        var notices = new HttpClient()
            .GetU8StringAsync("https://raw.githubusercontent.com/dotnet/runtime/main/THIRD-PARTY-NOTICES.TXT")
            .GetAwaiter()
            .GetResult();

        ThirdPartyNotices = notices[..Length];
        ThirdPartyNoticesU16 = notices[..Length].ToString();
    }

    [Benchmark]
    public int CountRunes() => ThirdPartyNotices.Runes.Count;

    [Benchmark]
    public int CountRunesForeach()
    {
        var res = 0;
        foreach (var _ in ThirdPartyNotices.Runes)
        {
            res++;
        }

        return res;
    }

    [Benchmark]
    public int CountRunesUtf16Span()
    {
        var res = 0;
        foreach (var _ in ThirdPartyNoticesU16.AsSpan().EnumerateRunes())
        {
            res++;
        }

        return res;
    }

    [Benchmark]
    public int CountRunesUtf16() => ThirdPartyNoticesU16!.EnumerateRunes().Count();

    [Benchmark]
    public int CountLines() => ThirdPartyNotices.Lines.Count;

    [Benchmark]
    public int CountLinesForeach()
    {
        var res = 0;
        foreach (var line in ThirdPartyNotices.Lines)
        {
            res++;
        }

        return res;
    }

    [Benchmark]
    public int CountLinesUtf16Span()
    {
        var res = 0;
        foreach (var _ in ThirdPartyNoticesU16.AsSpan().EnumerateLines())
        {
            res++;
        }

        return res;
    }

    // Different behavior
    [Benchmark]
    public int CountLinesUtf16Split() => ThirdPartyNoticesU16!.Split('\n').Length;
}
