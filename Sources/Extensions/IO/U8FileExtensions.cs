using Microsoft.Win32.SafeHandles;

namespace U8.IO;

public static class U8FileExtensions
{
    // TODO: Handle possible scenario where length == 0 for files that are unseekable or of unknown length
    // TODO: Detect and strip BOMs. Check if this requires BE to LE conversion.
    public static U8String ReadToU8String(this SafeFileHandle handle, long offset = 0)
    {
        if (offset < 0)
        {
            ThrowHelpers.ArgumentOutOfRange(nameof(offset));
        }

        var length = RandomAccess.GetLength(handle);
        if (length < offset)
        {
            ThrowHelpers.ArgumentOutOfRange(nameof(offset));
        }

        length -= offset;
        if (length > int.MaxValue)
        {
            ThrowHelpers.ArgumentException("File or file segment is too large to read into a U8String.");
        }

        if (length > 0)
        {
            // TODO: Should we just read the first int.MaxValue bytes?
            var buffer = new byte[int.CreateSaturating(length + 1)];
            var bytesStart = 0;
            var bytesRead = RandomAccess.Read(handle, buffer, offset);

            if (HasBOM(buffer))
            {
                bytesStart = 3;
                bytesRead -= 3;
            }

            U8String.Validate(buffer.SliceUnsafe(bytesStart, bytesRead));
            return new U8String(buffer, bytesStart, bytesRead);
        }

        return default;
    }

    public static async Task<U8String> ReadToU8StringAsync(
        this SafeFileHandle handle,
        long offset = 0,
        CancellationToken ct = default)
    {
        if (offset < 0)
        {
            ThrowHelpers.ArgumentOutOfRange(nameof(offset));
        }

        var length = RandomAccess.GetLength(handle);
        if (length < offset)
        {
            ThrowHelpers.ArgumentOutOfRange(nameof(offset));
        }

        length -= offset;
        if (length > int.MaxValue)
        {
            ThrowHelpers.ArgumentException("File or file segment is too large to read into a U8String.");
        }

        if (length > 0)
        {
            var buffer = new byte[int.CreateSaturating(length + 1)];
            var bytesStart = 0;
            var bytesRead = await RandomAccess
                .ReadAsync(handle, buffer, offset, ct)
                .ConfigureAwait(false);

            if (HasBOM(buffer))
            {
                bytesStart = 3;
                bytesRead -= 3;
            }

            U8String.Validate(buffer.SliceUnsafe(bytesStart, bytesRead));
            return new U8String(buffer, bytesStart, bytesRead);
        }

        return default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool HasBOM(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length >= 3)
        {
            ref var ptr = ref buffer.AsRef();

            var b01 = ptr.Cast<byte, ushort>();
            var b2 = ptr.Add(2);

            return b01 is (0xEF | 0xBB << 8) && b2 is 0xBF;
        }

        return false;
    }
}
