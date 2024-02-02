using System.Text;

using BenchmarkDotNet.Attributes;

using U8.Primitives;

namespace U8.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class Builder
{
    [Params(1, 10, 100)]
    public int N;

    [Benchmark]
    public U8String SimpleConstant()
    {
        var count = N;
        var builder = new U8Builder();
        for (var i = 0; i < count; i++)
        {
            builder.Append(u8("Hello, World!"));
        }

        return builder.Consume();
    }

    [Benchmark]
    public U8String SimpleLiteral()
    {
        var count = N;
        var builder = new U8Builder();
        for (var i = 0; i < count; i++)
        {
            builder.Append("Hello, World!"u8);
        }

        return builder.Consume();
    }

    [Benchmark]
    public U8String SimpleInterpolatedHandler()
    {
        var count = N;
        var handler = new InterpolatedU8StringHandler();
        for (var i = 0; i < count; i++)
        {
            handler.AppendFormatted(u8("Hello, World!"));
        }

        return new(ref handler);
    }

    [Benchmark]
    public string SimpleU16()
    {
        var count = N;
        var builder = new StringBuilder();
        for (var i = 0; i < count; i++)
        {
            builder.Append("Hello, World!");
        }

        return builder.ToString();
    }
}
