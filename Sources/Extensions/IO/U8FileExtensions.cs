using Microsoft.Win32.SafeHandles;

namespace U8Primitives.IO;

public static class U8FileExtensions
{
    // TODO: Handle possible scenario where length == 0 for files that are unseekable or of unknown length
    public static U8String ReadToU8String(this SafeFileHandle handle, long offset = 0)
    {
        var length = RandomAccess.GetLength(handle) - offset;
        if (length > int.MaxValue)
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        if (length > 0)
        {
            // TODO: Should we just read the first int.MaxValue bytes?
            var buffer = new byte[(int)length + 1];
            var bytesRead = RandomAccess.Read(handle, buffer, offset);

            U8String.Validate(buffer.SliceUnsafe(0, bytesRead));
            return new U8String(buffer, 0, bytesRead);
        }

        return default;
    }

    public static async Task<U8String> ReadToU8StringAsync(
        this SafeFileHandle handle,
        long offset = 0,
        CancellationToken ct = default)
    {
        var length = RandomAccess.GetLength(handle) - offset;
        if (length > int.MaxValue)
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        if (length > 0)
        {
            var buffer = new byte[(int)length + 1];
            var bytesRead = await RandomAccess.ReadAsync(handle, buffer, offset, ct).ConfigureAwait(false);

            U8String.Validate(buffer.SliceUnsafe(0, bytesRead));
            return new U8String(buffer, 0, bytesRead);
        }

        return default;
    }
}
