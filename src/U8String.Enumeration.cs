namespace U8Primitives;

public readonly partial struct U8String
{
    // Bad codegen, replace with custom implementation
    // Provide class variant for the interface implementation
    // [MethodImpl(MethodImplOptions.AggressiveInlining)]
    // public ReadOnlySpan<byte>.Enumerator GetEnumerator() => AsSpan().GetEnumerator();
}
