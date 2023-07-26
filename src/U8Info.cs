namespace U8Primitives;

static class U8Info
{
    // From https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Text/Unicode/Utf8Utility.Helpers.cs#L393C40-L393C40
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsContinuationByte(in byte value)
    {
        return (sbyte)value < -64;
    }
}
