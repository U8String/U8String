namespace U8Primitives.IO;

internal static class U8StreamExtensions
{
    public static U8String ReadToU8String(this Stream stream)
    {
        var length = stream.Length - stream.Position;
        if (length > int.MaxValue)
        {
            // TODO: EH UX
            ThrowHelpers.ArgumentOutOfRange();
        }

        if (length > 0)
        {
            // TODO: Verify correct implementation behavior
            var buffer = new byte[(int)length + 1];
            var bytesRead = stream.Read(buffer);

            U8String.Validate(buffer.SliceUnsafe(0, bytesRead));
            return new U8String(buffer, 0, bytesRead);
        }

        return default;
    }

    public static async Task<U8String> ReadToU8StringAsync(this Stream stream, CancellationToken ct = default)
    {
        var length = stream.Length - stream.Position;
        if (length > int.MaxValue)
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        if (length > 0)
        {
            var buffer = new byte[(int)length + 1];
            var bytesRead = await stream.ReadAsync(buffer, ct).ConfigureAwait(false);

            U8String.Validate(buffer.SliceUnsafe(0, bytesRead));
            return new U8String(buffer, 0, bytesRead);
        }

        return default;
    }
}
