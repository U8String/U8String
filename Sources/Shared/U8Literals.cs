namespace U8Primitives;

static partial class U8Literals
{
    static class Boolean
    {
        // Can't use static readonly U8String here because NativeAOT doesn't suppport
        // preinit for byte arrays nested in structs as of .NET 8. But constructing
        // U8String around byte[] readonly static is even better codegen-wise.
        static readonly byte[] True = [(byte)'T', (byte)'r', (byte)'u', (byte)'e', (byte)'\0'];
        static readonly byte[] False = [(byte)'F', (byte)'a', (byte)'l', (byte)'s', (byte)'e', (byte)'\0'];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static U8String Get(bool value)
        {
            return value ? new(True, 0, 4) : new(False, 0, 5);
        }
    }

    internal static U8String GetBoolean(bool value) => Boolean.Get(value);
    internal static U8String GetByte(byte value) => Numbers.Get(value);
    internal static bool TryGetInt8(sbyte value, out U8String literal) => Numbers.TryGet(value, out literal);
    internal static bool TryGetInt16(short value, out U8String literal) => Numbers.TryGet(value, out literal);
    internal static bool TryGetUInt16(ushort value, out U8String literal) => Numbers.TryGet(value, out literal);
    internal static bool TryGetInt32(int value, out U8String literal) => Numbers.TryGet(value, out literal);
    internal static bool TryGetUInt32(uint value, out U8String literal) => Numbers.TryGet((nint)value, out literal);
    internal static bool TryGetInt64(long value, out U8String literal) => Numbers.TryGet((nint)value, out literal);
    internal static bool TryGetUInt64(ulong value, out U8String literal) => Numbers.TryGet(nint.CreateSaturating(value), out literal);
}
