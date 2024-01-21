// This file contains modified code from dotnet/runtime licensed under the MIT license.
// See THIRD-PARTY-NOTICES.txt in the root of this repository for terms.
using System.Runtime.InteropServices;

namespace U8.Polyfills.Text;

#pragma warning disable IDE0007 // Use implicit type. Why: Source format.
internal static partial class Ascii
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe nuint GetIndexOfFirstNonAsciiByte(ReadOnlySpan<byte> value)
    {
        fixed (byte* pValue = &MemoryMarshal.GetReference(value))
        {
            return GetIndexOfFirstNonAsciiByte(pValue, (nuint)value.Length);
        }
    }
}
