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
}
