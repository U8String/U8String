using System.Text;

using BenchmarkDotNet.Attributes;

using U8.Primitives;

namespace U8.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 1, exportCombinedDisassemblyReport: true)]
// [ShortRunJob, ShortRunJob(RuntimeMoniker.NativeAot80)]
public class Enumeration
{
    [Params(100, 1000, int.MaxValue)]
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

        ThirdPartyNotices = notices.SliceRounding(0, Length);
        ThirdPartyNoticesU16 = ThirdPartyNotices.ToString();
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

    [Benchmark]
    public int CountLinesSplit() => ThirdPartyNotices.Split('\n').Count;

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
    public int EnumerateLinesSplit()
    {
        var res = 0;
        foreach (var line in ThirdPartyNotices.Split('\n'))
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
    public U8Slices CollectLinesSlices() => ThirdPartyNotices.Lines.ToSlices();

    [Benchmark]
    public U8String[] CollectLinesSplit() => ThirdPartyNotices.Split('\n').ToArray();

    [Benchmark]
    public U8Slices CollectLinesSplitSlices() => ThirdPartyNotices.Split('\n').ToSlices();

    [Benchmark]
    public string[] CollectLinesUtf16Split() => ThirdPartyNoticesU16!.Split('\n');
}
