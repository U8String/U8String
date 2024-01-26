using System.Collections;
using System.Diagnostics;
using System.Text;

using Microsoft.Win32.SafeHandles;

using U8.Abstractions;
using U8.Primitives;

namespace U8.IO;

public static class U8File
{
    public static U8FileLines ReadLines(string path)
    {
        ThrowHelpers.CheckNull(path);

        return new U8FileLines(path);
    }
}

// TODO: Is incompatible with unseekable files - fix this.
// TODO: Strip byte order mark
public class U8FileReader : IDisposable
{
    readonly SafeFileHandle _handle;
    readonly byte[] _buffer;
    long _position;

    int _bytesRead;
    int _bytesConsumed;

    public U8FileReader()
    {
        throw new InvalidOperationException();
    }

    public U8FileReader(SafeFileHandle handle)
    {
        ThrowHelpers.CheckNull(handle);

        _handle = handle;
        _buffer = new byte[4096];
    }

    public U8String? Read(int length, bool roundOffsets = false)
    {
        // var bytes = new byte[(nint)(uint)length + 1];
        // var buffer = bytes.AsSpan();
        // var bytesRead = _stream.Read(buffer);

        throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String? ReadLine()
    {
        var line = ReadTo((byte)'\n');
        if (line.HasValue)
        {
            line = line.Value.StripSuffix((byte)'\r');
        }

        return line;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public U8String? ReadTo(byte delimiter)
    {
        var unread = Fill();
        if (unread is [])
        {
            return null;
        }

        // Try to find the delimiter within the first read and
        // copy it directly to the result.
        var index = unread.IndexOf(delimiter);
        if (index >= 0)
        {
            Advance(index + 1);
            return new(unread.SliceUnsafe(0, index));
        }

        var builder = new InterpolatedU8StringHandler(unread.Length);
        Advance(unread.Length);
        builder.AppendBytes(unread);

        while ((unread = Fill()).Length > 0)
        {
            if ((index = unread.IndexOf(delimiter)) >= 0)
            {
                Advance(index + 1);
                builder.AppendBytes(unread.SliceUnsafe(0, index));
                break;
            }

            Advance(unread.Length);
            builder.AppendBytes(unread);
        }

        U8String.Validate(builder.Written);
        return new(ref builder);
    }

    public U8String? ReadTo(char delimiter)
    {
        ThrowHelpers.CheckSurrogate(delimiter);

        return char.IsAscii(delimiter)
            ? ReadTo((byte)delimiter)
            : ReadTo(delimiter <= 0x7ff ? delimiter.AsTwoBytes() : delimiter.AsThreeBytes());
    }

    public U8String? ReadTo(Rune delimiter)
    {
        return delimiter.IsAscii
            ? ReadTo((byte)delimiter.Value)
            : ReadTo(delimiter.Value switch
            {
                <= 0x7ff => delimiter.AsTwoBytes(),
                <= 0xffff => delimiter.AsThreeBytes(),
                _ => delimiter.AsFourBytes()
            });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public U8String? ReadTo(ReadOnlySpan<byte> delimiter)
    {
        if (delimiter.Length <= 0)
        {
            ThrowHelpers.ArgumentException();
        }

        var unread = Fill();
        if (unread is [])
        {
            return null;
        }

        // Try to find the delimiter within the first read and
        // copy it directly to the result.
        var index = unread.IndexOf(delimiter);
        if (index >= 0)
        {
            Advance(index + 1);
            return new(unread.SliceUnsafe(0, index));
        }

        var builder = new InterpolatedU8StringHandler(unread.Length);
        Advance(unread.Length);
        builder.AppendBytes(unread);

        while ((unread = Fill()).Length > 0)
        {
            if ((index = unread.IndexOf(delimiter)) >= 0)
            {
                Advance(index + 1);
                builder.AppendBytes(unread.SliceUnsafe(0, index));
                break;
            }

            Advance(unread.Length);
            builder.AppendBytes(unread);
        }

        U8String.Validate(builder.Written);
        return new(ref builder);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public U8String? ReadToAny(ReadOnlySpan<byte> delimiters)
    {
        if (delimiters.IsEmpty)
        {
            ThrowHelpers.ArgumentException();
        }

        var unread = Fill();
        if (unread is [])
        {
            return null;
        }

        // Try to find the delimiter within the first read and
        // copy it directly to the result.
        var index = unread.IndexOfAny(delimiters);
        if (index >= 0)
        {
            Advance(index + 1);
            return new(unread.SliceUnsafe(0, index));
        }

        var builder = new InterpolatedU8StringHandler(unread.Length);
        Advance(unread.Length);
        builder.AppendBytes(unread);

        while ((unread = Fill()).Length > 0)
        {
            if ((index = unread.IndexOfAny(delimiters)) >= 0)
            {
                Advance(index + 1);
                builder.AppendBytes(unread.SliceUnsafe(0, index));
                break;
            }

            Advance(unread.Length);
            builder.AppendBytes(unread);
        }

        U8String.Validate(builder.Written);
        return new(ref builder);
    }

    public U8String? ReadToEnd()
    {
        throw new NotImplementedException();
    }

    public U8ReadResult TryRead(int length, out U8String value)
    {
        throw new NotImplementedException();
    }

    void Advance(int length)
    {
        _bytesConsumed += length;
    }

    Span<byte> Fill()
    {
        var buffer = _buffer;
        var position = _position;
        var read = _bytesRead;
        var consumed = _bytesConsumed;

        // Possibly re-copying the data due to how
        // Fill() is consumed is *much* cheaper than
        // issuing empty reads at the end of the file.
        if (consumed < read)
        {
            // Nothing to fill, the reader must consume
            // the unread bytes first.
            goto Done;
        }

        // Check if we need to reset the buffer
        if (read >= buffer.Length)
        {
            Debug.Assert(consumed == read);
            read = 0;
            consumed = 0;
        }

        var filled = RandomAccess.Read(
            _handle, buffer.SliceUnsafe(read, buffer.Length - read), position);
        read += filled;

        _bytesRead = read;
        _bytesConsumed = consumed;
        _position = position + filled;

    Done:
        return buffer.AsSpan(consumed, read - consumed);
    }

    public void Dispose()
    {
        _handle.Dispose();
        GC.SuppressFinalize(this);
    }
}

public readonly struct U8FileLines(string path) : IU8Enumerable<U8FileLines.Enumerator>
{
    public Enumerator GetEnumerator() => new(path);

    IEnumerator<U8String> IEnumerable<U8String>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public struct Enumerator(string path) : IU8Enumerator
    {
        readonly U8FileReader _reader = new(
            File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.Read));

        public U8String Current { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            var line = _reader.ReadTo((byte)'\n');
            if (line.HasValue)
            {
                Current = line.Value.StripSuffix((byte)'\r');
                return true;
            }

            return false;
        }

        public readonly void Dispose() => _reader.Dispose();

        readonly object IEnumerator.Current => Current;
        readonly void IEnumerator.Reset() => throw new NotSupportedException();
    }
}
