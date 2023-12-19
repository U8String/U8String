using System.Text.Json;
using System.Text.Json.Serialization;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace U8.Benchmarks;

[MemoryDiagnoser]
// [DisassemblyDiagnoser(maxDepth: 2)]
[ShortRunJob, ShortRunJob(RuntimeMoniker.NativeAot80)]
public class Serialization
{
    public record Person
    {
        public required string FirstName { get; init; }
        public required string LastName { get; init; }
        public required string Title { get; init; }
        public required string ID { get; init; }
    }

    public record PersonU8
    {
        public required U8String FirstName { get; init; }
        public required U8String LastName { get; init; }
        public required U8String Title { get; init; }
        public required U8String ID { get; init; }
    }

    private static readonly Person PersonValue = new()
    {
        FirstName = "John",
        LastName = "Doe",
        Title = "Software Engineer",
        ID = "123456789"
    };

    private static readonly PersonU8 PersonU8Value = new()
    {
        FirstName = U8String.CreateUnchecked("John"u8),
        LastName = U8String.CreateUnchecked("Doe"u8),
        Title = U8String.CreateUnchecked("Software Engineer"u8),
        ID = U8String.CreateUnchecked("123456789"u8)
    };

    private static readonly U8String PersonBytes = U8String
        .Serialize(PersonValue, JsonContext.Default.Person);

    [Benchmark(Baseline = true)]
    public Person? DeserializePerson() =>
        JsonSerializer.Deserialize(PersonBytes, JsonContext.Default.Person);

    [Benchmark]
    public PersonU8? DeserializePersonU8() =>
        JsonSerializer.Deserialize(PersonBytes, JsonContext.Default.PersonU8);

    [Benchmark]
    public string SerializePerson() =>
        JsonSerializer.Serialize(PersonValue, JsonContext.Default.Person);

    [Benchmark]
    public string SerializePersonU8() =>
        JsonSerializer.Serialize(PersonU8Value, JsonContext.Default.PersonU8);

    [Benchmark]
    public U8String SerializePersonToUtf8() =>
        U8String.Serialize(PersonValue, JsonContext.Default.Person);

    [Benchmark]
    public U8String SerializePersonU8ToUtf8() =>
        U8String.Serialize(PersonU8Value, JsonContext.Default.PersonU8);
}

[JsonSerializable(typeof(Serialization.Person))]
[JsonSerializable(typeof(Serialization.PersonU8))]
public sealed partial class JsonContext : JsonSerializerContext { }