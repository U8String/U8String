using BenchmarkDotNet.Attributes;

namespace U8Primitives.Benchmarks;

[MemoryDiagnoser]
// [ShortRunJob, ShortRunJob(RuntimeMoniker.NativeAot80)]
public class Enumeration
{
    [Params(100, 1000, 100000)]
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

        notices += notices;

        ThirdPartyNotices = notices[..Length];
        ThirdPartyNoticesU16 = notices[..Length].ToString();
    }

    [Benchmark]
    public int CountChars() => ThirdPartyNotices.Chars.Count;

    [Benchmark]
    public int EnumerateChars()
    {
        var res = 0;
        foreach (var _ in ThirdPartyNotices.Chars)
        {
            res++;
        }

        return res;
    }

    [Benchmark]
    public char[] CollectChars() => ThirdPartyNotices.Chars.ToArray();

    [Benchmark]
    public int CountRunes() => ThirdPartyNotices.Runes.Count;

    [Benchmark]
    public int CountRunesUtf16() => ThirdPartyNoticesU16!.EnumerateRunes().Count();

    [Benchmark]
    public int EnumerateRunes()
    {
        var res = 0;
        foreach (var _ in ThirdPartyNotices.Runes)
        {
            res++;
        }

        return res;
    }

    [Benchmark]
    public int EnumerateRunesUtf16Span()
    {
        var res = 0;
        foreach (var _ in ThirdPartyNoticesU16.AsSpan().EnumerateRunes())
        {
            res++;
        }

        return res;
    }

    [Benchmark]
    public Rune[] CollectRunes() => ThirdPartyNotices.Runes.ToArray();

    [Benchmark]
    public Rune[] CollectRunesUtf16() => ThirdPartyNoticesU16!.EnumerateRunes().ToArray();

    [Benchmark]
    public int CountLines() => ThirdPartyNotices.Lines.Count;

    // Different behavior
    [Benchmark]
    public int CountLinesUtf16Split() => ThirdPartyNoticesU16!.Split('\n').Length;

    [Benchmark]
    public int EnumerateLines()
    {
        var res = 0;
        foreach (var line in ThirdPartyNotices.Lines)
        {
            res++;
        }

        return res;
    }

    [Benchmark]
    public int EnumerateLinesUtf16Span()
    {
        var res = 0;
        foreach (var _ in ThirdPartyNoticesU16.AsSpan().EnumerateLines())
        {
            res++;
        }

        return res;
    }

    [Benchmark]
    public U8String[] CollectLines() => ThirdPartyNotices.Lines.ToArray();

    [Benchmark]
    public string[] CollectLinesUtf16Split() => ThirdPartyNoticesU16!.Split('\n');
}
