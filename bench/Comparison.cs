using BenchmarkDotNet.Attributes;

namespace U8Primitives.Benchmarks;

[ShortRunJob]
public class Comparison
{
    public IEnumerable<U8String> Values()
    {
        yield return (U8String)"Hello, World!"u8;
        yield return (U8String)"Привіт, Всесвіт!"u8;
        yield return new HttpClient()
            .GetU8StringAsync("https://raw.githubusercontent.com/dotnet/runtime/main/THIRD-PARTY-NOTICES.TXT")
            .GetAwaiter()
            .GetResult()
            .Slice(0, 10_000);
    }

    [ParamsSource(nameof(Values))]
    public U8String Left;

    U8String Right;
    string? Left16;
    string? Right16;

    [GlobalSetup]
    public void Setup()
    {
        Right = Left.Clone();
        Left16 = Left.ToString();
        Right16 = Right.ToString();
    }

    [Benchmark]
    public bool Equals() => Left.Equals(Right);

    [Benchmark]
    public bool EqualsUtf16() => Left16!.Equals(Right16);

    [Benchmark]
    public bool EqualsAsciiIgnoreCase() => Left.Equals(Right, U8Comparison.AsciiIgnoreCase);

    [Benchmark]
    public bool EqualsAsciiIgnoreCaseUtf16() => Left16!.Equals(Right16, StringComparison.OrdinalIgnoreCase);
}