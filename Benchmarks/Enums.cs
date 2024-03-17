using System.Collections.Immutable;
using System.Net;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace U8.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
// [DisassemblyDiagnoser(maxDepth: 2, exportCombinedDisassemblyReport: true)]
public class Enums
{
    readonly HttpStatusCode EnumValue = HttpStatusCode.BadRequest;
    readonly U8String HttpStatusCodeValue = HttpStatusCode.BadRequest.ToU8String();
    readonly string HttpStatusCodeValueU16 = nameof(HttpStatusCode.BadRequest);
    readonly U8String HttpStatusCodeCapsValue = HttpStatusCode.BadRequest.ToU8String().ToUpperAscii();
    readonly string HttpStatusCodeCapsValueU16 = nameof(HttpStatusCode.BadRequest).ToUpper();
    readonly U8String HttpStatusCodeNotDefined = ((HttpStatusCode)1337).ToU8String();
    readonly string HttpStatusCodeNotDefinedU16 = ((HttpStatusCode)1337).ToString();

    [Benchmark]
    public ImmutableArray<U8String> GetNames() => U8Enum.GetNames<HttpStatusCode>();

    [Benchmark]
    public string[] GetNamesU16() => Enum.GetNames<HttpStatusCode>();

    [Benchmark]
    public ImmutableArray<HttpStatusCode> GetValues() => U8Enum.GetValues<HttpStatusCode>();

    [Benchmark]
    public HttpStatusCode[] GetValuesU16() => Enum.GetValues<HttpStatusCode>();

    [Benchmark]
    public HttpStatusCode Parse() => U8Enum.Parse<HttpStatusCode>(HttpStatusCodeValue);

    [Benchmark]
    public HttpStatusCode ParseU16() => Enum.Parse<HttpStatusCode>(HttpStatusCodeValueU16);

    [Benchmark]
    public HttpStatusCode ParseIgnoreCase() => U8Enum.Parse<HttpStatusCode>(HttpStatusCodeCapsValue, ignoreCase: true);

    [Benchmark]
    public HttpStatusCode ParseIgnoreCaseU16() => Enum.Parse<HttpStatusCode>(HttpStatusCodeCapsValueU16, ignoreCase: true);

    [Benchmark]
    public HttpStatusCode ParseRare() => U8Enum.Parse<HttpStatusCode>(HttpStatusCodeNotDefined, U8EnumParseOptions.AllowNumericValues);

    [Benchmark]
    public HttpStatusCode ParseRareU16() => Enum.Parse<HttpStatusCode>(HttpStatusCodeNotDefinedU16);

    [Benchmark]
    public U8String? GetName() => U8Enum.GetName(EnumValue);

    [Benchmark]
    public U8String GetNameConstantContiguous() => DayOfWeek.Saturday.ToU8String();

    [Benchmark]
    public string? GetNameU16() => Enum.GetName(EnumValue);

    [Benchmark]
    public U8String ToStringDefined() => EnumValue.ToU8String();

    [Benchmark]
    public string ToStringDefinedU16() => EnumValue.ToString();

    [Benchmark]
    public U8String ToStringUndefined() => ((HttpStatusCode)1337).ToU8String();

    [Benchmark]
    public string ToStringUndefinedU16() => ((HttpStatusCode)1337).ToString();
}
