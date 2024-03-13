using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;

using U8.Serialization;

namespace U8.Tests.U8StringTests;

public partial class Serialization
{
    record MultipleProps(
        U8String Empty,
        U8String Value1,
        U8String Value2,
        U8String Value3);

    record struct MultiplePropsStruct(
        U8String Empty,
        U8String Value1,
        U8String Value2,
        U8String Value3);

    record NullableProps(
        U8String? Value1,
        U8String? Value2);

    record struct NullablePropsStruct(
        U8String? Value1,
        U8String? Value2);

    [Fact]
    public void JsonSerializer_SerializesMultiplePropsCorrectly()
    {
        var expected = new MultipleProps(
            default,
            new(Constants.AsciiLetters),
            new(Constants.CyrilicBytes),
            new(Constants.KanaBytes));

        var utf8Json = expected.ToU8Json(SerializerContext.Configured.MultipleProps);
        var utf16Json = JsonSerializer.Serialize(expected, SerializerContext.Configured.MultipleProps);

        var actualFromUtf8 = JsonSerializer.Deserialize(utf8Json, SerializerContext.Configured.MultipleProps)!;
        var actualFromUtf16 = JsonSerializer.Deserialize(utf16Json, SerializerContext.Configured.MultipleProps)!;

        foreach (var actual in new[] { actualFromUtf8, actualFromUtf16 })
        {
            Assert.Equal(expected, actual);

            Assert.True(actual.Empty.IsEmpty);
            Assert.True(actual.Value1.IsNullTerminated);
            Assert.True(actual.Value2.IsNullTerminated);
            Assert.True(actual.Value3.IsNullTerminated);
        }
    }

    [Fact]
    public void JsonSerializer_SerializesMultiplePropsStructCorrectly()
    {
        var expected = new MultiplePropsStruct(
            default,
            new(Constants.AsciiLetters),
            new(Constants.CyrilicBytes),
            new(Constants.KanaBytes));

        var utf8Json = expected.ToU8Json(SerializerContext.Configured.MultiplePropsStruct);
        var utf16Json = JsonSerializer.Serialize(expected, SerializerContext.Configured.MultiplePropsStruct);

        var actualFromUtf8 = JsonSerializer.Deserialize(utf8Json, SerializerContext.Configured.MultiplePropsStruct);
        var actualFromUtf16 = JsonSerializer.Deserialize(utf16Json, SerializerContext.Configured.MultiplePropsStruct);

        foreach (var actual in new[] { actualFromUtf8, actualFromUtf16 })
        {
            Assert.Equal(expected, actual);

            Assert.True(actual.Empty.IsEmpty);
            Assert.True(actual.Value1.IsNullTerminated);
            Assert.True(actual.Value2.IsNullTerminated);
            Assert.True(actual.Value3.IsNullTerminated);
        }
    }

    [Fact]
    public void JsonSerializer_SerializesNullablePropsCorrectly()
    {
        var expected = new NullableProps(null, default(U8String));

        var utf8Json = expected.ToU8Json(SerializerContext.Configured.NullableProps);
        var utf16Json = JsonSerializer.Serialize(expected, SerializerContext.Configured.NullableProps);

        var serializedUtf8 = JsonSerializer.Deserialize(utf8Json, SerializerContext.Configured.NullableProps)!;
        var serializedUtf16 = JsonSerializer.Deserialize(utf16Json, SerializerContext.Configured.NullableProps)!;

        foreach (var actual in new[] { serializedUtf8, serializedUtf16 })
        {
            Assert.Equal(expected, actual);

            Assert.Null(actual.Value1);
            Assert.NotNull(actual.Value2);
            Assert.True(actual.Value2.Value.IsEmpty);
        }
    }

    [Fact]
    public void JsonSerializer_SerializesNullablePropsStructCorrectly()
    {
        var expected = new NullablePropsStruct(null, default(U8String));

        var utf8Json = expected.ToU8Json(SerializerContext.Configured.NullablePropsStruct);
        var utf16Json = JsonSerializer.Serialize(expected, SerializerContext.Configured.NullablePropsStruct);

        var serializedUtf8 = JsonSerializer.Deserialize(utf8Json, SerializerContext.Configured.NullablePropsStruct);
        var serializedUtf16 = JsonSerializer.Deserialize(utf16Json, SerializerContext.Configured.NullablePropsStruct);

        foreach (var actual in new[] { serializedUtf8, serializedUtf16 })
        {
            Assert.Equal(expected, actual);

            Assert.Null(actual.Value1);
            Assert.NotNull(actual.Value2);
            Assert.True(actual.Value2.Value.IsEmpty);
        }
    }

    [JsonSerializable(typeof(MultipleProps))]
    [JsonSerializable(typeof(MultiplePropsStruct))]
    [JsonSerializable(typeof(NullableProps))]
    [JsonSerializable(typeof(NullablePropsStruct))]
    [JsonSourceGenerationOptions]
    partial class SerializerContext : JsonSerializerContext
    {
        public static readonly SerializerContext Configured = new(new()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        });
    }
}
