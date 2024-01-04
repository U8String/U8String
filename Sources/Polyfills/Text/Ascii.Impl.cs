// This file contains modified code from dotnet/runtime licensed under the MIT license.
// See THIRD-PARTY-NOTICES.txt in the root of this repository for terms.
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace U8.Polyfills.Text;

#pragma warning disable IDE0007, IDE1006, RCS1211 // Use implicit type and explicit branch ordering. Why: Source format and codegen shape.
internal static partial class Ascii
{
    /// <summary>
    /// Returns <see langword="true"/> iff all bytes in <paramref name="value"/> are ASCII.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AllBytesInUInt64AreAscii(ulong value)
    {
        // If the high bit of any byte is set, that byte is non-ASCII.

        return (value & UInt64HighBitsOnlyMask) == 0;
    }

    /// <summary>
    /// Returns <see langword="true"/> iff all chars in <paramref name="value"/> are ASCII.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AllCharsInUInt32AreAscii(uint value)
    {
        return (value & ~0x007F007Fu) == 0;
    }

    /// <summary>
    /// Returns <see langword="true"/> iff all chars in <paramref name="value"/> are ASCII.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AllCharsInUInt64AreAscii(ulong value)
    {
        return (value & ~0x007F007F_007F007Ful) == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AllCharsInUInt64AreAscii<T>(ulong value)
        where T : unmanaged
    {
        Debug.Assert(typeof(T) == typeof(byte) || typeof(T) == typeof(ushort));

        return typeof(T) == typeof(byte)
            ? AllBytesInUInt64AreAscii(value)
            : AllCharsInUInt64AreAscii(value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetIndexOfFirstNonAsciiByteInLane_AdvSimd(Vector128<byte> value, Vector128<byte> bitmask)
    {
        if (!AdvSimd.Arm64.IsSupported || !BitConverter.IsLittleEndian)
        {
            throw new PlatformNotSupportedException();
        }

        // extractedBits[i] = (value[i] >> 7) & (1 << (12 * (i % 2)));
        var mostSignificantBitIsSet = AdvSimd.ShiftRightArithmetic(value.AsSByte(), 7).AsByte();
        var extractedBits = AdvSimd.And(mostSignificantBitIsSet, bitmask);

        // collapse mask to lower bits
        extractedBits = AdvSimd.Arm64.AddPairwise(extractedBits, extractedBits);
        var mask = extractedBits.AsUInt64().ToScalar();

        // calculate the index
        var index = BitOperations.TrailingZeroCount(mask) >> 2;
        Debug.Assert((mask != 0) ? index < 16 : index >= 16);
        return index;
    }

    /// <summary>
    /// Given a DWORD which represents two packed chars in machine-endian order,
    /// <see langword="true"/> iff the first char (in machine-endian order) is ASCII.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool FirstCharInUInt32IsAscii(uint value)
    {
        return (BitConverter.IsLittleEndian && (value & 0xFF80u) == 0)
            || (!BitConverter.IsLittleEndian && (value & 0xFF800000u) == 0);
    }

    /// <summary>
    /// Returns the index in <paramref name="pBuffer"/> where the first non-ASCII byte is found.
    /// Returns <paramref name="bufferLength"/> if the buffer is empty or all-ASCII.
    /// </summary>
    /// <returns>An ASCII byte is defined as 0x00 - 0x7F, inclusive.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static unsafe nuint GetIndexOfFirstNonAsciiByte(byte* pBuffer, nuint bufferLength)
    {
        // If SSE2 is supported, use those specific intrinsics instead of the generic vectorized
        // code below. This has two benefits: (a) we can take advantage of specific instructions like
        // pmovmskb which we know are optimized, and (b) we can avoid downclocking the processor while
        // this method is running.

        if (Vector512.IsHardwareAccelerated ||
            Vector256.IsHardwareAccelerated || (
            Vector128.IsHardwareAccelerated && BitConverter.IsLittleEndian))
        {
            return GetIndexOfFirstNonAsciiByte_Vector(pBuffer, bufferLength);
        }
        else
        {
            return GetIndexOfFirstNonAsciiByte_Default(pBuffer, bufferLength);
        }
    }

    private static unsafe nuint GetIndexOfFirstNonAsciiByte_Vector(byte* pBuffer, nuint bufferLength)
    {
        // Squirrel away the original buffer reference. This method works by determining the exact
        // byte reference where non-ASCII data begins, so we need this base value to perform the
        // final subtraction at the end of the method to get the index into the original buffer.

        var pOriginalBuffer = pBuffer;

        // Before we drain off byte-by-byte, try a generic vectorized loop.
        // Only run the loop if we have at least two vectors we can pull out.
        // Note use of SBYTE instead of BYTE below; we're using the two's-complement
        // representation of negative integers to act as a surrogate for "is ASCII?".

        if (Vector512.IsHardwareAccelerated && bufferLength >= 2 * (uint)Vector512<byte>.Count)
        {
            if (Vector512.Load(pBuffer).ExtractMostSignificantBits() == 0)
            {
                // The first several elements of the input buffer were ASCII. Bump up the pointer to the
                // next aligned boundary, then perform aligned reads from here on out until we find non-ASCII
                // data or we approach the end of the buffer. It's possible we'll reread data; this is ok.

                var pFinalVectorReadPos = pBuffer + bufferLength - Vector512Size;
                pBuffer = (byte*)(((nuint)pBuffer + Vector512Size) & ~(nuint)(Vector512Size - 1));

#if DEBUG
                var numBytesRead = pBuffer - pOriginalBuffer;
                Debug.Assert(0 < numBytesRead && numBytesRead <= Vector512Size, "We should've made forward progress of at least one byte.");
                Debug.Assert((nuint)numBytesRead <= bufferLength, "We shouldn't have read past the end of the input buffer.");
#endif

                Debug.Assert(pBuffer <= pFinalVectorReadPos, "Should be able to read at least one vector.");

                do
                {
                    Debug.Assert((nuint)pBuffer % Vector512Size == 0, "Vector read should be aligned.");
                    if (Vector512.LoadAligned(pBuffer).ExtractMostSignificantBits() != 0)
                    {
                        break; // found non-ASCII data
                    }

                    pBuffer += Vector512Size;
                } while (pBuffer <= pFinalVectorReadPos);

                // Adjust the remaining buffer length for the number of elements we just consumed.

                bufferLength -= (nuint)pBuffer;
                bufferLength += (nuint)pOriginalBuffer;
            }
        }
        else if (Vector256.IsHardwareAccelerated && bufferLength >= 2 * (uint)Vector256<byte>.Count)
        {
            if (Vector256.Load(pBuffer).ExtractMostSignificantBits() == 0)
            {
                // The first several elements of the input buffer were ASCII. Bump up the pointer to the
                // next aligned boundary, then perform aligned reads from here on out until we find non-ASCII
                // data or we approach the end of the buffer. It's possible we'll reread data; this is ok.

                var pFinalVectorReadPos = pBuffer + bufferLength - Vector256Size;
                pBuffer = (byte*)(((nuint)pBuffer + Vector256Size) & ~(nuint)(Vector256Size - 1));

#if DEBUG
                var numBytesRead = pBuffer - pOriginalBuffer;
                Debug.Assert(numBytesRead is > 0 and <= Vector256Size, "We should've made forward progress of at least one byte.");
                Debug.Assert((nuint)numBytesRead <= bufferLength, "We shouldn't have read past the end of the input buffer.");
#endif

                Debug.Assert(pBuffer <= pFinalVectorReadPos, "Should be able to read at least one vector.");

                do
                {
                    Debug.Assert((nuint)pBuffer % Vector256Size == 0, "Vector read should be aligned.");
                    if (Vector256.LoadAligned(pBuffer).ExtractMostSignificantBits() != 0)
                    {
                        break; // found non-ASCII data
                    }

                    pBuffer += Vector256Size;
                } while (pBuffer <= pFinalVectorReadPos);

                // Adjust the remaining buffer length for the number of elements we just consumed.

                bufferLength -= (nuint)pBuffer;
                bufferLength += (nuint)pOriginalBuffer;
            }
        }
        else if (Vector128.IsHardwareAccelerated && bufferLength >= 2 * (uint)Vector128<byte>.Count)
        {
            if (!VectorContainsNonAsciiChar(Vector128.Load(pBuffer)))
            {
                // The first several elements of the input buffer were ASCII. Bump up the pointer to the
                // next aligned boundary, then perform aligned reads from here on out until we find non-ASCII
                // data or we approach the end of the buffer. It's possible we'll reread data; this is ok.

                var pFinalVectorReadPos = pBuffer + bufferLength - Vector128Size;
                pBuffer = (byte*)(((nuint)pBuffer + Vector128Size) & ~(nuint)(Vector128Size - 1));

#if DEBUG
                var numBytesRead = pBuffer - pOriginalBuffer;
                Debug.Assert(0 < numBytesRead && numBytesRead <= Vector128Size, "We should've made forward progress of at least one byte.");
                Debug.Assert((nuint)numBytesRead <= bufferLength, "We shouldn't have read past the end of the input buffer.");
#endif

                Debug.Assert(pBuffer <= pFinalVectorReadPos, "Should be able to read at least one vector.");

                do
                {
                    Debug.Assert((nuint)pBuffer % Vector128Size == 0, "Vector read should be aligned.");
                    if (VectorContainsNonAsciiChar(Vector128.LoadAligned(pBuffer)))
                    {
                        break; // found non-ASCII data
                    }

                    pBuffer += Vector128Size;
                } while (pBuffer <= pFinalVectorReadPos);

                // Adjust the remaining buffer length for the number of elements we just consumed.

                bufferLength -= (nuint)pBuffer;
                bufferLength += (nuint)pOriginalBuffer;
            }
        }

        // At this point, the buffer length wasn't enough to perform a vectorized search, or we did perform
        // a vectorized search and encountered non-ASCII data. In either case go down a non-vectorized code
        // path to drain any remaining ASCII bytes.
        //
        // We're going to perform unaligned reads, so prefer 32-bit reads instead of 64-bit reads.
        // This also allows us to perform more optimized bit twiddling tricks to count the number of ASCII bytes.

        uint currentUInt32;

        // Try reading 64 bits at a time in a loop.

        for (; bufferLength >= 8; bufferLength -= 8)
        {
            currentUInt32 = Unsafe.ReadUnaligned<uint>(pBuffer);
            var nextUInt32 = Unsafe.ReadUnaligned<uint>(pBuffer + 4);

            if (!AllBytesInUInt32AreAscii(currentUInt32 | nextUInt32))
            {
                // One of these two values contains non-ASCII bytes.
                // Figure out which one it is, then put it in 'current' so that we can drain the ASCII bytes.

                if (AllBytesInUInt32AreAscii(currentUInt32))
                {
                    currentUInt32 = nextUInt32;
                    pBuffer += 4;
                }

                goto FoundNonAsciiData;
            }

            pBuffer += 8; // consumed 8 ASCII bytes
        }

        // From this point forward we don't need to update bufferLength.
        // Try reading 32 bits.

        if ((bufferLength & 4) != 0)
        {
            currentUInt32 = Unsafe.ReadUnaligned<uint>(pBuffer);
            if (!AllBytesInUInt32AreAscii(currentUInt32))
            {
                goto FoundNonAsciiData;
            }

            pBuffer += 4;
        }

        // Try reading 16 bits.

        if ((bufferLength & 2) != 0)
        {
            currentUInt32 = Unsafe.ReadUnaligned<ushort>(pBuffer);
            if (!AllBytesInUInt32AreAscii(currentUInt32))
            {
                if (!BitConverter.IsLittleEndian)
                {
                    currentUInt32 <<= 16;
                }
                goto FoundNonAsciiData;
            }

            pBuffer += 2;
        }

        // Try reading 8 bits

        if ((bufferLength & 1) != 0)
        {
            // If the buffer contains non-ASCII data, the comparison below will fail, and
            // we'll end up not incrementing the buffer reference.

            if (*(sbyte*)pBuffer >= 0)
            {
                pBuffer++;
            }
        }
        
    Finish:
        var totalNumBytesRead = (nuint)pBuffer - (nuint)pOriginalBuffer;
        return totalNumBytesRead;

    FoundNonAsciiData:

        Debug.Assert(!AllBytesInUInt32AreAscii(currentUInt32), "Shouldn't have reached this point if we have an all-ASCII input.");

        // The method being called doesn't bother looking at whether the high byte is ASCII. There are only
        // two scenarios: (a) either one of the earlier bytes is not ASCII and the search terminates before
        // we get to the high byte; or (b) all of the earlier bytes are ASCII, so the high byte must be
        // non-ASCII. In both cases we only care about the low 24 bits.

        pBuffer += CountNumberOfLeadingAsciiBytesFromUInt32WithSomeNonAsciiData(currentUInt32);
        goto Finish;
    }

    private static unsafe nuint GetIndexOfFirstNonAsciiByte_Default(byte* pBuffer, nuint bufferLength)
    {
        // Squirrel away the original buffer reference. This method works by determining the exact
        // byte reference where non-ASCII data begins, so we need this base value to perform the
        // final subtraction at the end of the method to get the index into the original buffer.

        var pOriginalBuffer = pBuffer;

        // Before we drain off byte-by-byte, try a generic vectorized loop.
        // Only run the loop if we have at least two vectors we can pull out.
        // Note use of SBYTE instead of BYTE below; we're using the two's-complement
        // representation of negative integers to act as a surrogate for "is ASCII?".

        if (Vector.IsHardwareAccelerated && bufferLength >= 2 * (uint)Vector<sbyte>.Count)
        {
            var SizeOfVectorInBytes = (uint)Vector<sbyte>.Count; // JIT will make this a const

            if (Vector.GreaterThanOrEqualAll(Unsafe.ReadUnaligned<Vector<sbyte>>(pBuffer), Vector<sbyte>.Zero))
            {
                // The first several elements of the input buffer were ASCII. Bump up the pointer to the
                // next aligned boundary, then perform aligned reads from here on out until we find non-ASCII
                // data or we approach the end of the buffer. It's possible we'll reread data; this is ok.

                var pFinalVectorReadPos = pBuffer + bufferLength - SizeOfVectorInBytes;
                pBuffer = (byte*)(((nuint)pBuffer + SizeOfVectorInBytes) & ~(nuint)(SizeOfVectorInBytes - 1));

#if DEBUG
                var numBytesRead = pBuffer - pOriginalBuffer;
                Debug.Assert(0 < numBytesRead && numBytesRead <= SizeOfVectorInBytes, "We should've made forward progress of at least one byte.");
                Debug.Assert((nuint)numBytesRead <= bufferLength, "We shouldn't have read past the end of the input buffer.");
#endif

                Debug.Assert(pBuffer <= pFinalVectorReadPos, "Should be able to read at least one vector.");

                do
                {
                    Debug.Assert((nuint)pBuffer % SizeOfVectorInBytes == 0, "Vector read should be aligned.");
                    if (Vector.LessThanAny(Unsafe.Read<Vector<sbyte>>(pBuffer), Vector<sbyte>.Zero))
                    {
                        break; // found non-ASCII data
                    }

                    pBuffer += SizeOfVectorInBytes;
                } while (pBuffer <= pFinalVectorReadPos);

                // Adjust the remaining buffer length for the number of elements we just consumed.

                bufferLength -= (nuint)pBuffer;
                bufferLength += (nuint)pOriginalBuffer;
            }
        }

        // At this point, the buffer length wasn't enough to perform a vectorized search, or we did perform
        // a vectorized search and encountered non-ASCII data. In either case go down a non-vectorized code
        // path to drain any remaining ASCII bytes.
        //
        // We're going to perform unaligned reads, so prefer 32-bit reads instead of 64-bit reads.
        // This also allows us to perform more optimized bit twiddling tricks to count the number of ASCII bytes.

        uint currentUInt32;

        // Try reading 64 bits at a time in a loop.

        for (; bufferLength >= 8; bufferLength -= 8)
        {
            currentUInt32 = Unsafe.ReadUnaligned<uint>(pBuffer);
            var nextUInt32 = Unsafe.ReadUnaligned<uint>(pBuffer + 4);

            if (!AllBytesInUInt32AreAscii(currentUInt32 | nextUInt32))
            {
                // One of these two values contains non-ASCII bytes.
                // Figure out which one it is, then put it in 'current' so that we can drain the ASCII bytes.

                if (AllBytesInUInt32AreAscii(currentUInt32))
                {
                    currentUInt32 = nextUInt32;
                    pBuffer += 4;
                }

                goto FoundNonAsciiData;
            }

            pBuffer += 8; // consumed 8 ASCII bytes
        }

        // From this point forward we don't need to update bufferLength.
        // Try reading 32 bits.

        if ((bufferLength & 4) != 0)
        {
            currentUInt32 = Unsafe.ReadUnaligned<uint>(pBuffer);
            if (!AllBytesInUInt32AreAscii(currentUInt32))
            {
                goto FoundNonAsciiData;
            }

            pBuffer += 4;
        }

        // Try reading 16 bits.

        if ((bufferLength & 2) != 0)
        {
            currentUInt32 = Unsafe.ReadUnaligned<ushort>(pBuffer);
            if (!AllBytesInUInt32AreAscii(currentUInt32))
            {
                if (!BitConverter.IsLittleEndian)
                {
                    currentUInt32 <<= 16;
                }
                goto FoundNonAsciiData;
            }

            pBuffer += 2;
        }

        // Try reading 8 bits

        if ((bufferLength & 1) != 0)
        {
            // If the buffer contains non-ASCII data, the comparison below will fail, and
            // we'll end up not incrementing the buffer reference.

            if (*(sbyte*)pBuffer >= 0)
            {
                pBuffer++;
            }
        }

    Finish:

        var totalNumBytesRead = (nuint)pBuffer - (nuint)pOriginalBuffer;
        return totalNumBytesRead;

    FoundNonAsciiData:

        Debug.Assert(!AllBytesInUInt32AreAscii(currentUInt32), "Shouldn't have reached this point if we have an all-ASCII input.");

        // The method being called doesn't bother looking at whether the high byte is ASCII. There are only
        // two scenarios: (a) either one of the earlier bytes is not ASCII and the search terminates before
        // we get to the high byte; or (b) all of the earlier bytes are ASCII, so the high byte must be
        // non-ASCII. In both cases we only care about the low 24 bits.

        pBuffer += CountNumberOfLeadingAsciiBytesFromUInt32WithSomeNonAsciiData(currentUInt32);
        goto Finish;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsNonAsciiByte_Sse2(uint sseMask)
    {
        Debug.Assert(sseMask != uint.MaxValue);
        Debug.Assert(Sse2.IsSupported);
        return sseMask != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ContainsNonAsciiByte_AdvSimd(uint advSimdIndex)
    {
        Debug.Assert(advSimdIndex != uint.MaxValue);
        Debug.Assert(AdvSimd.IsSupported);
        return advSimdIndex < 16;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool VectorContainsNonAsciiChar(Vector128<byte> asciiVector)
    {
        // max ASCII character is 0b_0111_1111, so the most significant bit (0x80) tells whether it contains non ascii

        // prefer architecture specific intrinsic as they offer better perf
        if (Sse41.IsSupported)
        {
            return !Sse41.TestZ(asciiVector, Vector128.Create((byte)0x80));
        }
        else if (AdvSimd.Arm64.IsSupported)
        {
            var maxBytes = AdvSimd.Arm64.MaxPairwise(asciiVector, asciiVector);
            return (maxBytes.AsUInt64().ToScalar() & 0x8080808080808080) != 0;
        }
        else
        {
            return asciiVector.ExtractMostSignificantBits() != 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool VectorContainsNonAsciiChar(Vector128<ushort> utf16Vector)
    {
        // prefer architecture specific intrinsic as they offer better perf
        if (Sse2.IsSupported)
        {
            if (Sse41.IsSupported)
            {
                var asciiMaskForTestZ = Vector128.Create((ushort)0xFF80);
                // If a non-ASCII bit is set in any WORD of the vector, we have seen non-ASCII data.
                return !Sse41.TestZ(utf16Vector.AsInt16(), asciiMaskForTestZ.AsInt16());
            }
            else
            {
                var asciiMaskForAddSaturate = Vector128.Create((ushort)0x7F80);
                // The operation below forces the 0x8000 bit of each WORD to be set iff the WORD element
                // has value >= 0x0800 (non-ASCII). Then we'll treat the vector as a BYTE vector in order
                // to extract the mask. Reminder: the 0x0080 bit of each WORD should be ignored.
                return (Sse2.MoveMask(Sse2.AddSaturate(utf16Vector, asciiMaskForAddSaturate).AsByte()) & 0b_1010_1010_1010_1010) != 0;
            }
        }
        else if (AdvSimd.Arm64.IsSupported)
        {
            // First we pick four chars, a larger one from all four pairs of adjecent chars in the vector.
            // If any of those four chars has a non-ASCII bit set, we have seen non-ASCII data.
            var maxChars = AdvSimd.Arm64.MaxPairwise(utf16Vector, utf16Vector);
            return (maxChars.AsUInt64().ToScalar() & 0xFF80FF80FF80FF80) != 0;
        }
        else
        {
            const ushort asciiMask = ushort.MaxValue - 127; // 0xFF80
            var zeroIsAscii = utf16Vector & Vector128.Create(asciiMask);
            // If a non-ASCII bit is set in any WORD of the vector, we have seen non-ASCII data.
            return zeroIsAscii != Vector128<ushort>.Zero;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool VectorContainsNonAsciiChar(Vector256<ushort> utf16Vector)
    {
        if (Avx.IsSupported)
        {
            var asciiMaskForTestZ = Vector256.Create((ushort)0xFF80);
            return !Avx.TestZ(utf16Vector.AsInt16(), asciiMaskForTestZ.AsInt16());
        }
        else
        {
            const ushort asciiMask = ushort.MaxValue - 127; // 0xFF80
            var zeroIsAscii = utf16Vector & Vector256.Create(asciiMask);
            // If a non-ASCII bit is set in any WORD of the vector, we have seen non-ASCII data.
            return zeroIsAscii != Vector256<ushort>.Zero;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool VectorContainsNonAsciiChar(Vector512<ushort> utf16Vector)
    {
        const ushort asciiMask = ushort.MaxValue - 127; // 0xFF80
        var zeroIsAscii = utf16Vector & Vector512.Create(asciiMask);
        // If a non-ASCII bit is set in any WORD of the vector, we have seen non-ASCII data.
        return zeroIsAscii != Vector512<ushort>.Zero;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool VectorContainsNonAsciiChar<T>(Vector128<T> vector)
        where T : unmanaged
    {
        Debug.Assert(typeof(T) == typeof(byte) || typeof(T) == typeof(ushort));

        return typeof(T) == typeof(byte)
            ? VectorContainsNonAsciiChar(vector.AsByte())
            : VectorContainsNonAsciiChar(vector.AsUInt16());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AllCharsInVectorAreAscii<T>(Vector128<T> vector)
        where T : unmanaged
    {
        Debug.Assert(typeof(T) == typeof(byte) || typeof(T) == typeof(ushort));

        // This is a copy of VectorContainsNonAsciiChar with an inverted condition.
        if (typeof(T) == typeof(byte))
        {
            return
                Sse41.IsSupported ? Sse41.TestZ(vector.AsByte(), Vector128.Create((byte)0x80)) :
                AdvSimd.Arm64.IsSupported ? AllBytesInUInt64AreAscii(AdvSimd.Arm64.MaxPairwise(vector.AsByte(), vector.AsByte()).AsUInt64().ToScalar()) :
                vector.AsByte().ExtractMostSignificantBits() == 0;
        }
        else
        {
            return
                Sse41.IsSupported ? Sse41.TestZ(vector.AsUInt16(), Vector128.Create((ushort)0xFF80)) :
                AdvSimd.Arm64.IsSupported ? AllCharsInUInt64AreAscii(AdvSimd.Arm64.MaxPairwise(vector.AsUInt16(), vector.AsUInt16()).AsUInt64().ToScalar()) :
                (vector.AsUInt16() & Vector128.Create((ushort)0xFF80)) == Vector128<ushort>.Zero;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AllCharsInVectorAreAscii<T>(Vector256<T> vector)
        where T : unmanaged
    {
        Debug.Assert(typeof(T) == typeof(byte) || typeof(T) == typeof(ushort));

        if (typeof(T) == typeof(byte))
        {
            return
                Avx.IsSupported ? Avx.TestZ(vector.AsByte(), Vector256.Create((byte)0x80)) :
                vector.AsByte().ExtractMostSignificantBits() == 0;
        }
        else
        {
            return
                Avx.IsSupported ? Avx.TestZ(vector.AsUInt16(), Vector256.Create((ushort)0xFF80)) :
                (vector.AsUInt16() & Vector256.Create((ushort)0xFF80)) == Vector256<ushort>.Zero;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AllCharsInVectorAreAscii<T>(Vector512<T> vector)
        where T : unmanaged
    {
        Debug.Assert(typeof(T) == typeof(byte) || typeof(T) == typeof(ushort));

        if (typeof(T) == typeof(byte))
        {
            return vector.AsByte().ExtractMostSignificantBits() == 0;
        }
        else
        {
            return (vector.AsUInt16() & Vector512.Create((ushort)0xFF80)) == Vector512<ushort>.Zero;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector128<byte> ExtractAsciiVector(Vector128<ushort> vectorFirst, Vector128<ushort> vectorSecond)
    {
        // Narrows two vectors of words [ w7 w6 w5 w4 w3 w2 w1 w0 ] and [ w7' w6' w5' w4' w3' w2' w1' w0' ]
        // to a vector of bytes [ b7 ... b0 b7' ... b0'].

        // prefer architecture specific intrinsic as they don't perform additional AND like Vector128.Narrow does
        if (Sse2.IsSupported)
        {
            return Sse2.PackUnsignedSaturate(vectorFirst.AsInt16(), vectorSecond.AsInt16());
        }
        else if (AdvSimd.Arm64.IsSupported)
        {
            return AdvSimd.Arm64.UnzipEven(vectorFirst.AsByte(), vectorSecond.AsByte());
        }
        else
        {
            return Vector128.Narrow(vectorFirst, vectorSecond);
        }
    }
}
