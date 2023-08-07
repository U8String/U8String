using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace U8Primitives.Benchmarks;

#pragma warning disable CS1718 // Comparison made to same variable. Why: Benchmark.
[ShortRunJob, ShortRunJob(RuntimeMoniker.NativeAot80)]
public class Equality
{
    static ReadOnlySpan<byte> Literal => "Привіт, Всесвіт!"u8;
    static readonly U8String JitConst = Literal;
    static readonly byte[] ByteJitConst = JitConst.ToArray();
    readonly U8String Instance = Literal;
    public U8String Source = Literal;

    const string LiteralUTF16 = "Привіт, Всесвіт!";
    static readonly string JitConstUTF16 = LiteralUTF16[0] + LiteralUTF16[1..];
    readonly string InstanceUTF16 = LiteralUTF16[0] + LiteralUTF16[1..];
    public string SourceUTF16 = LiteralUTF16[0] + LiteralUTF16[1..];

    [Benchmark]
    public bool Equals() => Source == Instance;

    [Benchmark]
    public bool EqualsUtf16() => SourceUTF16 == InstanceUTF16;

    [Benchmark]
    public bool EqualsLiteral() => Source == Literal;

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
