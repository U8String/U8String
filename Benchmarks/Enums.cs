using System.Collections.Immutable;

using BenchmarkDotNet.Attributes;

namespace U8.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 2, exportCombinedDisassemblyReport: true)]
public class Enums
{
    readonly DayOfWeek DayOfWeek = DayOfWeek.Saturday;
    readonly U8String DayOfWeekValue = DayOfWeek.Saturday.ToU8String();
    readonly string DayOfWeekValueU16 = nameof(DayOfWeek.Saturday);
    readonly U8String DayOfWeekCapsValue = DayOfWeek.Saturday.ToU8String().ToUpperAscii();
    readonly string DayOfWeekCapsValueU16 = nameof(DayOfWeek.Saturday).ToUpper();
    readonly U8String DayOfWeekNotDefined = ((DayOfWeek)1337).ToU8String();
    readonly string DayOfWeekNotDefinedU16 = ((DayOfWeek)1337).ToString();

    [Benchmark]
    public ImmutableArray<U8String> GetNames() => U8Enum.GetNames<DayOfWeek>();

    [Benchmark]
    public string[] GetNamesU16() => Enum.GetNames<DayOfWeek>();

    [Benchmark]
    public ImmutableArray<DayOfWeek> GetValues() => U8Enum.GetValues<DayOfWeek>();

    [Benchmark]
    public DayOfWeek[] GetValuesU16() => Enum.GetValues<DayOfWeek>();

    [Benchmark]
    public DayOfWeek Parse() => U8Enum.Parse<DayOfWeek>(DayOfWeekValue);

    [Benchmark]
    public DayOfWeek ParseU16() => Enum.Parse<DayOfWeek>(DayOfWeekValueU16);

    [Benchmark]
    public DayOfWeek ParseIgnoreCase() => U8Enum.Parse<DayOfWeek>(DayOfWeekCapsValue, ignoreCase: true);

    [Benchmark]
    public DayOfWeek ParseIgnoreCaseU16() => Enum.Parse<DayOfWeek>(DayOfWeekCapsValueU16, ignoreCase: true);

    [Benchmark]
    public DayOfWeek ParseRare() => U8Enum.Parse<DayOfWeek>(DayOfWeekNotDefined);

    [Benchmark]
    public DayOfWeek ParseRareU16() => Enum.Parse<DayOfWeek>(DayOfWeekNotDefinedU16);

    [Benchmark]
    public U8String Format() => DayOfWeek.ToU8String();

    [Benchmark]
    public string FormatU16() => DayOfWeek.ToString();

    [Benchmark]
    public U8String FormatRare() => ((DayOfWeek)1337).ToU8String();

    [Benchmark]
    public string FormatRareU16() => ((DayOfWeek)1337).ToString();
}
