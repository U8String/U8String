
using Microsoft.Win32.SafeHandles;

namespace U8.IO;

// TODO: Specialize ctors on U8Reader and stop requiring to manually specify the kind.
public static class U8ReaderSource
{
    // TODO: Disambiguate seekable and non-seekable file sources?
    // The underlying implementation does handle this but I think
    // a better job can be done especially when investigating miniPAL-like approach.
    public struct File(SafeFileHandle handle) : IU8ReaderSource<File>
    {
        readonly SafeFileHandle _handle = handle;

        long _offset;

        public File() : this(null!)
        {
            throw new InvalidOperationException();
        }

        public int Read(Span<byte> buffer)
        {
            var consumed = RandomAccess.Read(_handle, buffer, _offset);
            _offset += consumed;
            return consumed;
        }

        // TODO: This appears to be causing extra overhead due to +1 async method in the call stack.
        public static async ValueTask<int> ReadAsync(
            U8Reader<File> reader,
            Memory<byte> buffer,
            CancellationToken ct)
        {
            var consumed = await RandomAccess.ReadAsync(
                reader.Source._handle, buffer, reader.Source._offset, ct);
            reader.Source._offset += consumed;
            return consumed;
        }

        public void Dispose() => _handle.Dispose();
    }

    public struct Stream(System.IO.Stream stream) : IU8ReaderSource<Stream>
    {
        readonly System.IO.Stream _stream = stream;

        public Stream() : this(null!)
        {
            throw new InvalidOperationException();
        }

        public int Read(Span<byte> buffer) => _stream.Read(buffer);

        public static ValueTask<int> ReadAsync(
            U8Reader<Stream> reader,
            Memory<byte> buffer,
            CancellationToken ct)
        {
            return reader.Source._stream.ReadAsync(buffer, ct);
        }

        public void Dispose() => _stream.Dispose();
    }

}
