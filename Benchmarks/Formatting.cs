using BenchmarkDotNet.Attributes;

namespace U8.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
// [SimpleJob, SimpleJob(RuntimeMoniker.NativeAot80)]
[DisassemblyDiagnoser(maxDepth: 2, exportCombinedDisassemblyReport: true)]
public class Formatting
{
    static readonly DateTime DateTime = new(2021, 10, 10, 10, 10, 10, DateTimeKind.Utc);
    static readonly Guid Guid = Guid.NewGuid();
    const decimal Decimal = 42.42m;

    [Benchmark]
    public U8String FormatDateTime() => U8String.Format($"Date is {DateTime}");

    [Benchmark]
    public string FormatDateTimeU16() => $"Date is {DateTime}";

    [Benchmark]
    public U8String FormatEnum() => U8String.Format($"Enum is ${StringComparison.Ordinal}");

    [Benchmark]
    public string FormatEnumU16() => $"Enum is ${StringComparison.Ordinal}";

    [Benchmark]
    public U8String FormatBool() => U8String.Format($"{true} {false}");

    [Benchmark]
    public string FormatBoolU16() => $"{true} {false}";

    [Benchmark]
    public U8String FormatGuid() => U8String.Format($"Guid is {Guid}");

    [Benchmark]
    public string FormatGuidU16() => $"Guid is {Guid}";

    [Benchmark]
    public U8String FormatDecimal() => U8String.Format($"Decimal is {Decimal}");

    [Benchmark]
    public string FormatDecimalU16() => $"Decimal is {Decimal}";

    [Benchmark]
    public U8String FormatMany() => U8String.Format($"{DateTime} {Guid} {Decimal}");

    [Benchmark]
    public string FormatManyU16() => $"{DateTime} {Guid} {Decimal}";
}
