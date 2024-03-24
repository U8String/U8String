namespace U8.Shared;

static partial class U8Literals
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static U8String GetByte(byte value)
    {
        return Numbers.GetValueUnchecked(value);
    }
}
