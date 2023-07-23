using System.Text.Json;
using System.Text.Json.Serialization;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace U8Primitives.Benchmarks;

[MemoryDiagnoser]
[SimpleJob, SimpleJob(RuntimeMoniker.NativeAot80)]
public class Serialization
{
    public record Person(
        string FirstName,
        string LastName,
        string Title,
        string ID);

    public record PersonU8(
        U8String FirstName,
        U8String LastName,
        U8String Title,
        U8String ID);

    private static readonly Person PersonValue = new(
        "John", "Doe", "Software Enginer", "123456789");

    private static readonly PersonU8 PersonU8Value = new(
        new U8String("John"u8),
        new U8String("Doe"u8),
        new U8String("Software Enginer"u8),
        new U8String("123456789"u8));

    private static readonly byte[] PersonBytes = JsonSerializer
        .SerializeToUtf8Bytes(PersonValue, JsonContext.Default.Person);

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
    public byte[] SerializePersonToUtf8() =>
        JsonSerializer.SerializeToUtf8Bytes(PersonValue, JsonContext.Default.Person);

    [Benchmark]
    public byte[] SerializePersonU8ToUtf8() =>
        JsonSerializer.SerializeToUtf8Bytes(PersonU8Value, JsonContext.Default.PersonU8);
}

[JsonSerializable(typeof(Serialization.Person))]
[JsonSerializable(typeof(Serialization.PersonU8))]
public sealed partial class JsonContext : JsonSerializerContext { }