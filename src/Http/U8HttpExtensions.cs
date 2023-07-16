using U8Primitives;

namespace System.Net.Http;

/// <summary>
/// Provides extension methods to integrate <see cref="U8String"/> with the .NET type system.
/// </summary>
public static class U8HttpExtensions
{
    // TODO: Consider using "validating stream" to interrupt the request as soon as invalid UTF-8 is detected. Or not?
    public static async Task<U8String> GetU8StringAsync(this HttpClient client, string requestUri)
    {
        var bytes = await client.GetByteArrayAsync(requestUri);

        U8String.Validate(bytes);
        return new(bytes, 0, bytes.Length);
    }

    public static async Task<U8String> GetU8StringAsync(this HttpClient client, Uri requestUri)
    {
        var bytes = await client.GetByteArrayAsync(requestUri);

        U8String.Validate(bytes);
        return new(bytes, 0, bytes.Length);
    }

    public static async Task<U8String> GetU8StringAsync(
        this HttpClient client, string requestUri, CancellationToken cancellationToken)
    {
        var bytes = await client.GetByteArrayAsync(requestUri, cancellationToken);

        U8String.Validate(bytes);
        return new(bytes, 0, bytes.Length);
    }

    public static async Task<U8String> GetU8StringAsync(
        this HttpClient client, Uri requestUri, CancellationToken cancellationToken)
    {
        var bytes = await client.GetByteArrayAsync(requestUri, cancellationToken);

        U8String.Validate(bytes);
        return new(bytes, 0, bytes.Length);
    }

    public static async Task<U8String> ReadAsU8StringAsync(this HttpContent content)
    {
        var bytes = await content.ReadAsByteArrayAsync();

        U8String.Validate(bytes);
        return new(bytes, 0, bytes.Length);
    }

    public static async Task<U8String> ReadAsU8StringAsync(
        this HttpContent content, CancellationToken cancellationToken)
    {
        var bytes = await content.ReadAsByteArrayAsync(cancellationToken);

        U8String.Validate(bytes);
        return new(bytes, 0, bytes.Length);
    }
}
