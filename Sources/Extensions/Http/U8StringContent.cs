using System.Net.Http.Headers;

namespace U8Primitives.Http;

public sealed class U8StringContent : ByteArrayContent
{
    const string DefaultMediaType = "text/plain";
    const string CharSetType = "utf-8";

    public U8StringContent(U8String content)
        : base(content._value ?? Array.Empty<byte>(), content.Offset, content.Length)
    {
        Headers.ContentType = new(DefaultMediaType, CharSetType);
    }

    public U8StringContent(U8String content, string? mediaType)
        : base(content._value ?? Array.Empty<byte>(), content.Offset, content.Length)
    {
        Headers.ContentType = new(mediaType ?? DefaultMediaType, CharSetType);
    }

    public U8StringContent(U8String content, MediaTypeHeaderValue mediaType)
        : base(content._value ?? Array.Empty<byte>(), content.Offset, content.Length)
    {
        Headers.ContentType = mediaType;
    }
}
