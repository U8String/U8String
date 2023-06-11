using System.Runtime.CompilerServices;

namespace U8Primitives;

public readonly partial struct U8String
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte>.Enumerator GetEnumerator() => AsSpan().GetEnumerator();
}
