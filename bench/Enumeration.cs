using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using U8Primitives.InteropServices;

namespace U8Primitives.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob, ShortRunJob(RuntimeMoniker.NativeAot80)]
public class Enumeration
{
    private static readonly U8String License = U8Marshal.Create(
        File.ReadAllBytes("../THIRD-PARTY-NOTICES.txt"));

    private static readonly string LicenseUTF16 = License.ToString();

    private int LineCount;

    private static readonly char[] NewLineChars = "\n\r\f\u0085\u2028\u2029".ToArray();

    [GlobalSetup]
    public void Setup()
    {
        var res = 0;
        foreach (var _ in LicenseUTF16.AsSpan().EnumerateLines())
        {
            res++;
        }

        LineCount = res;
    }

    // TODO: Submit a bug report to dotnet/runtime because this is completely broken with FullOpts
    [Benchmark(Baseline = true)]
    public int Lines()
    {
        var res = 0;
        foreach (var line in License.Lines)
        {
            res += line.Length;
        }

        // ArgumentOutOfRangeException.ThrowIfNotEqual(res, TotalLength);
        return res;
    }

    [Benchmark]
    public int LinesEnumerable() => License.Lines.Count();

    [Benchmark]
    public int LinesUtf16Span()
    {
        var res = 0;
        foreach (var line in LicenseUTF16.AsSpan().EnumerateLines())
        {
            res += line.Length;
        }

        return res;
    }

    // Different behavior
    [Benchmark]
    public int LinesUtf16SplitCount() => LicenseUTF16.Split(NewLineChars).Length;
}
