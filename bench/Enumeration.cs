using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using U8Primitives.InteropServices;

namespace U8Primitives.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob, ShortRunJob(RuntimeMoniker.NativeAot80)]
public class Enumeration
{
    private static readonly U8String ThirdPartyNotices = new HttpClient()
        .GetU8StringAsync("https://raw.githubusercontent.com/dotnet/runtime/main/THIRD-PARTY-NOTICES.TXT")
        .GetAwaiter()
        .GetResult();

    private static readonly string ThirdPartyNoticesU16 = ThirdPartyNotices.ToString();

    private static readonly char[] NewLineChars = "\n\r".ToArray();

    [Benchmark]
    public int CountRunes() => ThirdPartyNotices.Runes.Count;

    [Benchmark]
    public int CountRunesUtf16() => ThirdPartyNoticesU16.EnumerateRunes().Count();

    [Benchmark]
    public int CountLines()
    {
        var res = 0;
        foreach (var line in ThirdPartyNotices.Lines)
        {
            res++;
        }

        return res;
    }

    [Benchmark]
    public int CountLinesEnumerable() => ThirdPartyNotices.Lines.Count();

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
    public int CountLinesUtf16Split() => ThirdPartyNoticesU16.Split(NewLineChars).Length;
}
