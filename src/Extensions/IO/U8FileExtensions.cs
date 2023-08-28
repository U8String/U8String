using Microsoft.Win32.SafeHandles;

namespace U8Primitives.IO;

public static class U8FileExtensions
{
    public static U8String ReadToU8String(this SafeFileHandle handle, long offset = 0)
    {
        var length = RandomAccess.GetLength(handle) - offset;
        if (length > int.MaxValue)
        {
            ThrowHelpers.ArgumentOutOfRange();
        }
        
        if (length > 0)
        {
            var buffer = new byte[(int)length];
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
            var buffer = new byte[(int)length];
            var bytesRead = await RandomAccess.ReadAsync(handle, buffer, offset, ct);

            U8String.Validate(buffer.SliceUnsafe(0, bytesRead));
            return new U8String(buffer, 0, bytesRead);
        }

        return default;
    }
}