using System.Diagnostics;

namespace U8Primitives;

public readonly struct U8Range
{
    internal readonly int Offset;
    internal readonly int Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8Range(int offset, int length)
    {
        Debug.Assert((uint)offset <= int.MaxValue);
        Debug.Assert((uint)length <= int.MaxValue);

        Offset = offset;
        Length = length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8Range Slice(U8String value, int start)
    {
        Debug.Assert((uint)start <= int.MaxValue);

        return new(value.Offset + start, value.Length - start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8Range Slice(U8String value, int start, int length)
    {
        Debug.Assert((uint)start <= int.MaxValue);
        Debug.Assert((uint)length <= int.MaxValue);

        return new(value.Offset + start, length);
    }
}
