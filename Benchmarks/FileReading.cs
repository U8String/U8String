using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;

using U8.InteropServices;
using U8.IO;

namespace U8.Benchmarks;

[GcServer(false)]
[ShortRunJob]
[MemoryDiagnoser]
[DisassemblyDiagnoser(maxDepth: 2, exportCombinedDisassemblyReport: true)]
public class FileReading
{
    [Params("Constitution.txt", "Numbers.txt", "Vectorization.txt")]
    public string Name = "";

    [Benchmark(Baseline = true)]
    public U8String ReadU8String()
    {
        return U8File.Read(Name);
    }

    [Benchmark]
    public void EnumerateLines()
    {
        foreach (var _ in File.ReadLines(Name)) ;
    }

    [Benchmark]
    public void EnumerateLinesU8()
    {
        foreach (var _ in U8File.ReadLines(Name)) ;
    }

    [Benchmark]
    public void EnumerateWordsU8()
    {
        foreach (var _ in U8File.OpenRead(Name).Split(' ')) ;
    }

    [Benchmark]
    public void EnumerateLinesILChert()
    {
        foreach (var _ in ILChertFile.ReadLines(Name)) ;
    }

    [Benchmark]
    public Task<U8String> ReadU8StringAsync()
    {
        return U8File.ReadAsync(Name);
    }

    [Benchmark]
    public async Task EnumerateLinesAsync()
    {
        await foreach (var _ in File.ReadLinesAsync(Name)) ;
    }

    [Benchmark]
    public async Task EnumerateLinesU8Async()
    {
        await foreach (var _ in U8File.ReadLines(Name)) ;
    }

    [Benchmark]
    public async Task EnumerateWordsAsyncU8()
    {
        await foreach (var _ in U8File.OpenRead(Name).Split(' ')) ;
    }

    [Benchmark]
    public async Task ILChertEnumerateLinesAsync()
    {
        await foreach (var _ in ILChertFile.ReadLinesAsync(Name)) ;
    }
}

// Courtesy of https://github.com/Ilchert
internal static class ILChertFile
{
    public static IEnumerable<U8String> ReadLines(string fileName)
    {
        using var fs = File.OpenRead(fileName);
        var readBuffer = new byte[4096].AsMemory();

        var endWithReturn = false;
        var consumed = 0;
        var remaining = 0;
        while ((consumed = fs.Read(readBuffer[remaining..].Span)) > 0)
        {
            var buffer = readBuffer[..(remaining + consumed)];
            while (TryReadLine(ref buffer, out var line, ref endWithReturn))
            {
                var length = int.CreateChecked(line.Length + 1);
                var data = new byte[length];

                line.CopyTo(data);
                U8String.Validate(data);

                yield return U8Marshal.CreateUnsafe(data, 0, length - 1);
            }

            buffer.CopyTo(readBuffer);
            remaining = buffer.Length;
        }
    }

    public static async IAsyncEnumerable<U8String> ReadLinesAsync(
        string fileName, [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var fs = File.OpenRead(fileName);
        var pipeReader = PipeReader.Create(fs);
        var endWithReturn = false;
        while (true)
        {
            var readResult = await pipeReader.ReadAsync(ct);
            var buffer = readResult.Buffer;
            while (TryReadLine(ref buffer, out var line, ref endWithReturn))
            {
                ct.ThrowIfCancellationRequested();
                var length = int.CreateChecked(line.Length + 1);
                var data = new byte[length];

                line.CopyTo(data);
                U8String.Validate(data);

                yield return U8Marshal.CreateUnsafe(data, 0, length - 1);
            }

            pipeReader.AdvanceTo(buffer.Start, buffer.End);

            if (readResult.IsCompleted)
                break;
        }
        await pipeReader.CompleteAsync();
    }

    private static bool TryReadLine(
        ref Memory<byte> buffer,
        out Memory<byte> line,
        ref bool endWithReturn)
    {
        var source = buffer;
        if (source.Length > 0)
        {
            var bufferSpan = source.Span;

            if (endWithReturn && bufferSpan[0] == (byte)'\n')
            {
                source = source[1..];
                bufferSpan = source.Span;
            }

            var indexOfEnd = bufferSpan.IndexOfAny("\r\n"u8);
            var offset = 0;
            if (indexOfEnd >= 0)
            {
                offset += indexOfEnd;
                if (bufferSpan[indexOfEnd] == (byte)'\r')
                {
                    if (bufferSpan.Length > indexOfEnd + 1 &&
                        bufferSpan[indexOfEnd + 1] == (byte)'\n')
                    {
                        offset++;
                    }
                    else
                    {
                        endWithReturn = true;
                    }
                }
                offset++;
                line = source[..indexOfEnd];
                buffer = source[offset..];
                return true;
            }

            line = default;
            return false;
        }

        line = default;
        return false;
    }

    private static bool TryReadLine(
        ref ReadOnlySequence<byte> buffer,
        out ReadOnlySequence<byte> line,
        ref bool endWithReturn)
    {
        // Look for a EOL in the buffer.            
        var reader = new SequenceReader<byte>(buffer);
        if (endWithReturn)
            reader.IsNext((byte)'\n', true);

        if (reader.TryReadToAny(out line, "\r\n"u8, false))
        {
            if (reader.IsNext((byte)'\r', true))
                endWithReturn = !reader.IsNext((byte)'\n', true);
            else
                reader.Advance(1);

            buffer = buffer.Slice(reader.Position);
            return true;
        }

        return false;
    }
}