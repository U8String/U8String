using System.Text;

using BenchmarkDotNet.Attributes;

using U8.CompilerServices;
using U8.Primitives;

namespace U8.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class Builders
{
    [Params(1, 10, 100, 1000)]
    public int N;

    [Benchmark]
    public U8String SimpleConstant()
    {
        var count = N;
        var builder = new U8Builder();
        for (var i = 0; i < count; i++)
        {
            builder.Append(u8("Hello, World!"));
            builder.Append(1337);
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
            U8Unchecked.AppendBytes(builder, "Hello, World!"u8);
            builder.Append(1337);
        }

        return builder.Consume();
    }

    [Benchmark]
    public U8String SimpleInterpolatedHandler()
    {
        var count = N;
        var handler = new InlineU8Builder();
        for (var i = 0; i < count; i++)
        {
            handler.AppendFormatted(u8("Hello, World!"));
            handler.AppendFormatted(1337);
        }

        return new(ref handler);
    }

    [Benchmark]
    public U8String SimplePooledHandler()
    {
        var count = N;
        var handler = new PooledU8Builder();
        for (var i = 0; i < count; i++)
        {
            handler.AppendFormatted(u8("Hello, World!"));
            handler.AppendFormatted(1337);
        }

        var result = new U8String(handler.Written, skipValidation: true);
        handler.Dispose();
        return result;
    }

    [Benchmark]
    public string SimpleU16()
    {
        var count = N;
        var builder = new StringBuilder();
        for (var i = 0; i < count; i++)
        {
            builder.Append("Hello, World!");
            builder.Append(1337);
        }

        return builder.ToString();
    }
}
