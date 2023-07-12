using BenchmarkDotNet.Attributes;

namespace U8Primitives.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 3, exportCombinedDisassemblyReport: true)]
public class Conversion
{
    private readonly DateTime Date = DateTime.UtcNow;
    private readonly TimeSpan Time = TimeSpan.FromHours(24);
    private readonly Guid Guid = Guid.NewGuid();
    private readonly string String = "Hello, World!";
    private readonly U8String U8String = new("Hello, World!"u8);
    private readonly long Long = 1234567890123456789;
    private readonly double Double = 1234567890.1234567890;

    [Benchmark]
    public U8String FromDateTime() => Date.ToU8String();

    [Benchmark]
    public U8String FromTimeSpan() => Time.ToU8String();

    [Benchmark]
    public U8String FromGuid() => Guid.ToU8String();

    [Benchmark]
    public U8String FromString() => String.ToU8String();

    [Benchmark]
    public U8String FromU8String() => U8String.ToU8String<U8String>();

    [Benchmark]
    public U8String FromLong() => Long.ToU8String();

    [Benchmark]
    public U8String FromDouble() => Double.ToU8String();
}
