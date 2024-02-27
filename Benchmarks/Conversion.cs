using System.Net;
using BenchmarkDotNet.Attributes;

namespace U8.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 3, exportCombinedDisassemblyReport: true)]
public class Conversion
{
    private readonly DateTime Date = DateTime.UtcNow;
    private readonly TimeSpan Time = TimeSpan.FromHours(24);
    private readonly Guid Guid = Guid.NewGuid();
    private readonly string String = "Hello, World!";
    private readonly U8String U8Str = u8("Hello, World!");
    private readonly char Char = 'Ї';
    private readonly int Int = 12345678;
    private readonly long Long = 1234567890123456789;
    private readonly double Double = 1234567890.1234567890;
    private readonly IPAddress IPv4 = IPAddress.Parse("127.0.0.1");
    private readonly IPAddress IPv6 = IPAddress.Parse("1050:0:0:0:5:600:300c:326b");

    [Benchmark]
    public U8String FromDateTime() => Date.ToU8String();

    [Benchmark]
    public string FromDateTimeBase() => Date.ToString();

    [Benchmark]
    public U8String FromTimeSpan() => Time.ToU8String();

    [Benchmark]
    public string FromTimeSpanBase() => Time.ToString();

    [Benchmark]
    public U8String FromGuid() => U8String.Create(Guid);

    [Benchmark]
    public string FromGuidBase() => Guid.ToString();

    [Benchmark]
    public U8String FromString() => String.ToU8String();

    [Benchmark]
    public string FromStringBase() => String.ToString();

    [Benchmark]
    public U8String FromU8String() => U8String.Create(U8Str);

    [Benchmark]
    public string FromU8StringBase() => U8Str.ToString();

    [Benchmark]
    public U8String FromChar() => Char.ToU8String();

    [Benchmark]
    public string FromCharBase() => Char.ToString();

    [Benchmark]
    public U8String FromInt() => Int.ToU8String();

    [Benchmark]
    public string FromIntBase() => Int.ToString();

    [Benchmark]
    public U8String FromLong() => Long.ToU8String();

    [Benchmark]
    public string FromLongBase() => Long.ToString();

    [Benchmark]
    public U8String FromDouble() => Double.ToU8String();

    [Benchmark]
    public string FromDoubleBase() => Double.ToString();

    [Benchmark]
    public U8String FromIPv4() => IPv4.ToU8String();

    [Benchmark]
    public string FromIPv4Base() => IPv4.ToString();

    [Benchmark]
    public U8String FromIPv6() => IPv6.ToU8String();

    [Benchmark]
    public string FromIPv6Base() => IPv6.ToString();
}
