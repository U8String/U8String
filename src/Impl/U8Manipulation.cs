namespace U8Primitives;

internal static class U8Manipulation
{
    internal static U8String ConcatUnchecked(ReadOnlySpan<byte> left, byte right)
    {
        var length = left.Length + 1;
        var value = new byte[length];

        left.CopyTo(value);
        value[length - 1] = right;

        return new U8String(value, 0, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static U8String ConcatUnchecked(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        var length = left.Length + right.Length;
        var value = new byte[length];

        left.CopyTo(value);
        right.CopyTo(value.SliceUnsafe(left.Length));

        return new U8String(value, 0, length);
    }
}
