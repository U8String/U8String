using U8Primitives;

namespace System.Net.Http;

/// <summary>
/// Provides extension methods to integrate <see cref="U8String"/> with the .NET type system.
/// </summary>
public static class U8HttpExtensions
{
    // TODO: Consider using "validating stream" to interrupt the request as soon as invalid UTF-8 is detected. Or not?
    /// <inheritdoc cref="GetU8StringAsync(HttpClient, Uri?, CancellationToken)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<U8String> GetU8StringAsync(this HttpClient client, string? requestUri)
    {
        return client.GetU8StringAsync(CreateUri(requestUri));
    }

    /// <inheritdoc cref="GetU8StringAsync(HttpClient, Uri?, CancellationToken)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<U8String> GetU8StringAsync(this HttpClient client, Uri? requestUri)
    {
        return client.GetU8StringAsync(requestUri, CancellationToken.None);
    }

    /// <inheritdoc cref="GetU8StringAsync(HttpClient, Uri?, CancellationToken)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<U8String> GetU8StringAsync(
        this HttpClient client, string? requestUri, CancellationToken cancellationToken)
    {
        return client.GetU8StringAsync(CreateUri(requestUri), cancellationToken);
    }

    /// <summary>
    /// Sends a GET request to the specified Uri and returns the response body as a <see cref="U8String"/>.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/> instance.</param>
    /// <param name="requestUri">The Uri the request is sent to.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    /// <exception cref="HttpRequestException">The HTTP response failed.</exception>
    /// <exception cref="TaskCanceledException">The request was canceled.</exception>
    /// <exception cref="FormatException">The response body is not a valid UTF-8 sequence.</exception>
    public static async Task<U8String> GetU8StringAsync(
        this HttpClient client, Uri? requestUri, CancellationToken cancellationToken)
    {
        var bytes = await client.GetByteArrayAsync(requestUri, cancellationToken).ConfigureAwait(false);

        U8String.Validate(bytes);
        return new(bytes, 0, bytes.Length);
    }

    /// <inheritdoc cref="ReadAsU8StringAsync(HttpContent, CancellationToken)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<U8String> ReadAsU8StringAsync(this HttpContent content)
    {
        return content.ReadAsU8StringAsync(CancellationToken.None);
    }

    /// <summary>
    /// Serialize the HTTP content to a <see cref="U8String"/> as an asynchronous operation.
    /// </summary>
    /// <param name="content">The HTTP content to serialize.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    /// <exception cref="TaskCanceledException">The request was canceled.</exception>
    /// <exception cref="FormatException">The response body is not a valid UTF-8 sequence.</exception>
    public static async Task<U8String> ReadAsU8StringAsync(
        this HttpContent content, CancellationToken cancellationToken)
    {
        var bytes = await content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);

        U8String.Validate(bytes);
        return new(bytes, 0, bytes.Length);
    }

    static Uri? CreateUri(string? requestUri)
    {
        return !string.IsNullOrEmpty(requestUri)
            ? new(requestUri, UriKind.RelativeOrAbsolute)
            : null;
    }
}
