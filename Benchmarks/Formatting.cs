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
    public U8String FormatDateTime() => new($"Date is {DateTime}");

    [Benchmark]
    public string FormatDateTimeU16() => $"Date is {DateTime}";

    [Benchmark]
    public U8String FormatEnum() => new($"Enum is ${StringComparison.Ordinal}");

    [Benchmark]
    public string FormatEnumU16() => $"Enum is ${StringComparison.Ordinal}";

    [Benchmark]
    public U8String FormatBool() => new($"{true} {false}");

    [Benchmark]
    public string FormatBoolU16() => $"{true} {false}";

    [Benchmark]
    public U8String FormatGuid() => new($"Guid is {Guid}");

    [Benchmark]
    public string FormatGuidU16() => $"Guid is {Guid}";

    [Benchmark]
    public U8String FormatDecimal() => new($"Decimal is {Decimal}");

    [Benchmark]
    public string FormatDecimalU16() => $"Decimal is {Decimal}";

    [Benchmark]
    public U8String FormatMany() => new($"{DateTime} {Guid} {Decimal}");

    [Benchmark]
    public string FormatManyU16() => $"{DateTime} {Guid} {Decimal}";
}
