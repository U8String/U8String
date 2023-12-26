namespace U8.Shared;

static partial class U8Literals
{
    static class Boolean
    {
        // Can't use static readonly U8String here because NativeAOT doesn't suppport
        // preinit for byte arrays nested in structs as of .NET 8. But constructing
        // U8String around byte[] readonly static is even better codegen-wise.
        internal static readonly byte[] True = [(byte)'T', (byte)'r', (byte)'u', (byte)'e', (byte)'\0'];
        internal static readonly byte[] False = [(byte)'F', (byte)'a', (byte)'l', (byte)'s', (byte)'e', (byte)'\0'];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static U8String GetBoolean(bool value)
    {
        return value ? new(Boolean.True, 0, 4) : new(Boolean.False, 0, 5);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static U8String GetByte(byte value)
    {
        return Numbers.GetValueUnchecked(value);
    }
}
