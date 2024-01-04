using U8.Primitives;

namespace U8;

public readonly partial struct U8String
{
    sealed class DebugView(U8String value)
    {
        public bool IsAscii => value.IsAscii();
        public bool IsEmpty => value.IsEmpty;
        public bool IsNullTerminated => value.IsNullTerminated;
        public int Length => value.Length;
        public U8RuneIndices Runes => value.RuneIndices;
    }
}