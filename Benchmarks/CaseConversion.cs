using BenchmarkDotNet.Attributes;

using U8.IO;

namespace U8.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 3, exportCombinedDisassemblyReport: true)]
public class CaseConversion
{
    [Params("Constitution.txt", "Numbers.txt", "Vectorization.txt", null)]
    public string? Path;

    U8String Text8;
    string? Text16;

    [GlobalSetup]
    public void Setup()
    {
        if (Path is null)
        {
            Text8 = u8("Hello, World! Hello, World!");
            Text16 = Text8.ToString();
            return;
        }

        Text8 = U8File.Read(Path!);
        Text16 = Text8.ToString();
    }

    [Benchmark]
    public U8String ToUpperAscii() => Text8.ToUpper(U8CaseConversion.Ascii);

    [Benchmark]
    public U8String ToUpperInvariant() => Text8.ToUpper(U8CaseConversion.Invariant);

    [Benchmark]
    public string ToUpperBase() => Text16!.ToUpperInvariant();
}
