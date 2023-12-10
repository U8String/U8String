using System.IO.Hashing;
using BenchmarkDotNet.Attributes;

namespace U8.Benchmarks;

// [ShortRunJob, ShortRunJob(RuntimeMoniker.NativeAot80)]
// [MemoryDiagnoser]
[ShortRunJob]
[DisassemblyDiagnoser(maxDepth: 3, exportCombinedDisassemblyReport: true)]
public class Hashing
{
    [Params(
        "",
        "test",
        "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed non risus. Suspendisse",
        "Привіт, Всесвіт!",
        null!
    )]
    public string? ValueUtf16 { get; set; }
    public U8String Value { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        ValueUtf16 ??= new HttpClient()
            .GetStringAsync("https://raw.githubusercontent.com/dotnet/runtime/main/THIRD-PARTY-NOTICES.TXT")
            .GetAwaiter()
            .GetResult();

        Value = new U8String(ValueUtf16);
    }

    [Benchmark(Baseline = true)]
    public int XXHash3() => Value.GetHashCode();

    [Benchmark]
    public int StringDefault() => ValueUtf16!.GetHashCode();

    [Benchmark]
    public int XXHash32() => (int)XxHash32.HashToUInt32(Value);

    [Benchmark]
    public int XXHash64()
    {
        var hash = XxHash64.HashToUInt64(Value);

        return ((int)hash) ^ (int)(hash >> 32);
    }
}
