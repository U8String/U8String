using System.Runtime.InteropServices;
using System.Text;

using U8.Shared;

namespace U8.Extensions;

public static class Memory
{
    public ref struct SpanRuneEnumerator
    {
        readonly ref byte _end;
        ref byte _ptr;

        public SpanRuneEnumerator(ReadOnlySpan<byte> source)
        {
            ref var ptr = ref MemoryMarshal.GetReference(source);

            _ptr = ref ptr;
            _end = ref ptr.Add(source.Length);
        }

        public Rune Current { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            ref var ptr = ref _ptr;
            if (ptr.LessThan(ref _end))
            {
                Current = U8Conversions.CodepointToRune(ref ptr, out var size);
                _ptr = ref ptr.Add(size);
                return true;
            }

            return false;
        }

        public readonly SpanRuneEnumerator GetEnumerator() => this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SpanRuneEnumerator EnumerateRunes(this ReadOnlySpan<byte> source)
    {
        return new SpanRuneEnumerator(source);
    }
}