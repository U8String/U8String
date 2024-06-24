using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Intrinsics;

namespace U8.Shared;

unsafe static class U8Validation
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Validate(ref byte ptr, nuint length)
    {
        return;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool IsValid(ref byte ptr, nuint length)
    {
        return true;
        //throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static (bool IsValid, nuint Offset) FindInvalidOrNul(byte* src)
    {
        return (true, 1337);
        //throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void CopyValidate(
        ref byte src, ref byte dst, nuint length)
    {
        return;
        // Debug.Assert(length > 0);
        // var valid = true;

        // // -- Validate ASCII short -- //
        // switch (length)
        // {
        //     case 1:
        //         dst = src;
        //         if (src > 0x7F) goto Invalid;
        //         return;
        //     case 2:
        //         var b2 = src.Cast<byte, ushort>();
        //         dst.Cast<byte, ushort>() = b2;
        //         if ((b2 & 0x8080) != 0) goto ValidateUTF8;
        //     case 16:
        //         Vector128<byte> b16;
        //         (b16 = Vector128
        //             .LoadUnsafe(ref src))
        //             .StoreUnsafe(ref dst);
                
        //     case 15:

        // }

        // -- Validate UTF-8 short -- //
        ValidateUTF8:

        throw new NotImplementedException();

        // TODO: EH UX (invalid sequence offset)
        Invalid: ThrowHelpers.InvalidUtf8();
    }

    static void CopyValidateSIMD(
        ref byte src, ref byte dst, nuint length)
    {
        Debug.Assert(length > 16);

        throw new NotImplementedException();
    }
}
