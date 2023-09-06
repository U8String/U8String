using System.Runtime.InteropServices;

namespace U8Primitives;

[InlineArray(128)]
internal struct InlineBuffer
{
    byte _element0;

    internal Span<byte> AsSpan()
    {
        return MemoryMarshal.CreateSpan(ref _element0, 128);
    }
}