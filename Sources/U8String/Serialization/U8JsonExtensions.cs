using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace U8.Serialization;

public static class U8JsonExtensions
{
    public static U8String ToU8Json<T>(this T value, JsonTypeInfo<T> info)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, info);
        return new(bytes, 0, bytes.Length);
    }
}
