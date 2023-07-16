using System.Net.Http.Headers;

using U8Primitives;

namespace System.Net.Http;

public sealed class U8StringContent : ByteArrayContent
{
    const string DefaultMediaType = "text/plain";
    const string CharSetType = "utf-8";

    public U8StringContent(U8String content)
        : base(content.Value ?? Array.Empty<byte>(), 0, content.Length)
    {
        Headers.ContentType = new(DefaultMediaType, CharSetType);
    }

    public U8StringContent(U8String content, string? mediaType)
        : base(content.Value ?? Array.Empty<byte>(), 0, content.Length)
    {
        Headers.ContentType = new(mediaType ?? DefaultMediaType, CharSetType);
    }

    public U8StringContent(U8String content, MediaTypeHeaderValue mediaType)
        : base(content.Value ?? Array.Empty<byte>(), 0, content.Length)
    {
        Headers.ContentType = mediaType;
    }
}
