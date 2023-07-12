using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using U8Primitives.Unsafe;

namespace U8Primitives.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob, ShortRunJob(RuntimeMoniker.NativeAot80)]
public class Enumeration
{
    private static readonly U8String License = U8Marshal.Create(
        File.ReadAllBytes("/Users/arseniy/Code/GitHub/U8String/THIRD-PARTY-NOTICES.txt"));

    private static readonly string LicenseUTF16 = License.ToString();

    private static readonly char[] NewLineChars = "\n\r\f\u0085\u2028\u2029".ToArray();

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

    [Benchmark]
    public void LinesUtf16Span()
    {
        foreach (var line in LicenseUTF16.AsSpan().EnumerateLines())
        {
            _ = line;
        }
    }

    [Benchmark]
    public int LinesUtf16SplitCount() => LicenseUTF16.Split(NewLineChars).Length;
}
