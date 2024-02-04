using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using U8.InteropServices;

namespace U8.Benchmarks;

#pragma warning disable CS1718 // Comparison made to same variable. Why: Benchmark.
[ShortRunJob]
public class Equality
{
    static ReadOnlySpan<byte> SpanLiteral => "Привіт, Всесвіт!"u8;
    static readonly U8String JitConst = 'П' + u8("ривіт, Всесвіт!");
    static readonly byte[] ByteJitConst = JitConst.ToArray();
    readonly U8String Instance = 'П' + u8("ривіт, Всесвіт!");
    public U8String Source = 'П' + u8("ривіт, Всесвіт!");

    const string LiteralUTF16 = "Привіт, Всесвіт!";
    static readonly string JitConstUTF16 = LiteralUTF16[0] + LiteralUTF16[1..];
    readonly string InstanceUTF16 = LiteralUTF16[0] + LiteralUTF16[1..];
    public string SourceUTF16 = LiteralUTF16[0] + LiteralUTF16[1..];

    [Benchmark]
    public bool Equals() => Source == Instance;

    [Benchmark]
    public bool EqualsUtf16() => SourceUTF16 == InstanceUTF16;

    [Benchmark]
    public bool EqualsLiteral() => Source == u8("Привіт, Всесвіт!");

    [Benchmark]
    public bool EqualsSpanLiteral() => Source == SpanLiteral;

    [Benchmark]
    public bool EqualsLiteralUtf16() => SourceUTF16 == LiteralUTF16;

    [Benchmark]
    public bool EqualsJitConst() => Source == JitConst;

    [Benchmark]
    public bool EqualsByteJitConst() => Source == ByteJitConst;

    [Benchmark]
    public bool EqualsJitConstUtf16() => SourceUTF16 == JitConstUTF16;

    [Benchmark]
    public bool EqualsSelf() => Source == Source;

    [Benchmark]
    public bool EqualsSelfUtf16() => SourceUTF16 == SourceUTF16;
}
