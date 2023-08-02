// This file contains modified code from dotnet/runtime licensed under the MIT license.
// See THIRD-PARTY-NOTICES.txt in the root of this repository for terms.
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace U8Primitives.Polyfills.Text;

#pragma warning disable IDE0007 // Use implicit type. Why: Source format.
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
        Vector128<byte> mostSignificantBitIsSet = AdvSimd.ShiftRightArithmetic(value.AsSByte(), 7).AsByte();
        Vector128<byte> extractedBits = AdvSimd.And(mostSignificantBitIsSet, bitmask);

        // collapse mask to lower bits
        extractedBits = AdvSimd.Arm64.AddPairwise(extractedBits, extractedBits);
        ulong mask = extractedBits.AsUInt64().ToScalar();

        // calculate the index
        int index = BitOperations.TrailingZeroCount(mask) >> 2;
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
            Vector256.IsHardwareAccelerated ||
            Vector128.IsHardwareAccelerated)
        {
            return GetIndexOfFirstNonAsciiByte_Vector(pBuffer, bufferLength);
        }
        else if (Sse2.IsSupported || (AdvSimd.IsSupported && BitConverter.IsLittleEndian))
        {
            return GetIndexOfFirstNonAsciiByte_Intrinsified(pBuffer, bufferLength);
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

        byte* pOriginalBuffer = pBuffer;

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

                byte* pFinalVectorReadPos = pBuffer + bufferLength - Vector512Size;
                pBuffer = (byte*)(((nuint)pBuffer + Vector512Size) & ~(nuint)(Vector512Size - 1));

#if DEBUG
                long numBytesRead = pBuffer - pOriginalBuffer;
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

                byte* pFinalVectorReadPos = pBuffer + bufferLength - Vector256Size;
                pBuffer = (byte*)(((nuint)pBuffer + Vector256Size) & ~(nuint)(Vector256Size - 1));

#if DEBUG
                long numBytesRead = pBuffer - pOriginalBuffer;
                Debug.Assert(0 < numBytesRead && numBytesRead <= Vector256Size, "We should've made forward progress of at least one byte.");
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

                byte* pFinalVectorReadPos = pBuffer + bufferLength - Vector128Size;
                pBuffer = (byte*)(((nuint)pBuffer + Vector128Size) & ~(nuint)(Vector128Size - 1));

#if DEBUG
                long numBytesRead = pBuffer - pOriginalBuffer;
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
            uint nextUInt32 = Unsafe.ReadUnaligned<uint>(pBuffer + 4);

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

        nuint totalNumBytesRead = (nuint)pBuffer - (nuint)pOriginalBuffer;
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

        byte* pOriginalBuffer = pBuffer;

        // Before we drain off byte-by-byte, try a generic vectorized loop.
        // Only run the loop if we have at least two vectors we can pull out.
        // Note use of SBYTE instead of BYTE below; we're using the two's-complement
        // representation of negative integers to act as a surrogate for "is ASCII?".

        if (Vector.IsHardwareAccelerated && bufferLength >= 2 * (uint)Vector<sbyte>.Count)
        {
            uint SizeOfVectorInBytes = (uint)Vector<sbyte>.Count; // JIT will make this a const

            if (Vector.GreaterThanOrEqualAll(Unsafe.ReadUnaligned<Vector<sbyte>>(pBuffer), Vector<sbyte>.Zero))
            {
                // The first several elements of the input buffer were ASCII. Bump up the pointer to the
                // next aligned boundary, then perform aligned reads from here on out until we find non-ASCII
                // data or we approach the end of the buffer. It's possible we'll reread data; this is ok.

                byte* pFinalVectorReadPos = pBuffer + bufferLength - SizeOfVectorInBytes;
                pBuffer = (byte*)(((nuint)pBuffer + SizeOfVectorInBytes) & ~(nuint)(SizeOfVectorInBytes - 1));

#if DEBUG
                long numBytesRead = pBuffer - pOriginalBuffer;
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
            uint nextUInt32 = Unsafe.ReadUnaligned<uint>(pBuffer + 4);

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

        nuint totalNumBytesRead = (nuint)pBuffer - (nuint)pOriginalBuffer;
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

    private static unsafe nuint GetIndexOfFirstNonAsciiByte_Intrinsified(byte* pBuffer, nuint bufferLength)
    {
        // JIT turns the below into constants

        uint SizeOfVector128 = (uint)sizeof(Vector128<byte>);
        nuint MaskOfAllBitsInVector128 = (nuint)(SizeOfVector128 - 1);

        Debug.Assert(Sse2.IsSupported || AdvSimd.Arm64.IsSupported, "Sse2 or AdvSimd64 required.");
        Debug.Assert(BitConverter.IsLittleEndian, "This SSE2/Arm64 implementation assumes little-endian.");

        Vector128<byte> bitmask = BitConverter.IsLittleEndian ?
            Vector128.Create((ushort)0x1001).AsByte() :
            Vector128.Create((ushort)0x0110).AsByte();

        uint currentSseMask = uint.MaxValue, secondSseMask = uint.MaxValue;
        uint currentAdvSimdIndex = uint.MaxValue, secondAdvSimdIndex = uint.MaxValue;
        byte* pOriginalBuffer = pBuffer;

        // This method is written such that control generally flows top-to-bottom, avoiding
        // jumps as much as possible in the optimistic case of a large enough buffer and
        // "all ASCII". If we see non-ASCII data, we jump out of the hot paths to targets
        // after all the main logic.

        if (bufferLength < SizeOfVector128)
        {
            goto InputBufferLessThanOneVectorInLength; // can't vectorize; drain primitives instead
        }

        // Read the first vector unaligned.

        if (Sse2.IsSupported)
        {
            currentSseMask = (uint)Sse2.MoveMask(Sse2.LoadVector128(pBuffer)); // unaligned load
            if (ContainsNonAsciiByte_Sse2(currentSseMask))
            {
                goto FoundNonAsciiDataInCurrentChunk;
            }
        }
        else if (AdvSimd.Arm64.IsSupported)
        {
            currentAdvSimdIndex = (uint)GetIndexOfFirstNonAsciiByteInLane_AdvSimd(AdvSimd.LoadVector128(pBuffer), bitmask); // unaligned load
            if (ContainsNonAsciiByte_AdvSimd(currentAdvSimdIndex))
            {
                goto FoundNonAsciiDataInCurrentChunk;
            }
        }
        else
        {
            throw new PlatformNotSupportedException();
        }

        // If we have less than 32 bytes to process, just go straight to the final unaligned
        // read. There's no need to mess with the loop logic in the middle of this method.

        if (bufferLength < 2 * SizeOfVector128)
        {
            goto IncrementCurrentOffsetBeforeFinalUnalignedVectorRead;
        }

        // Now adjust the read pointer so that future reads are aligned.

        pBuffer = (byte*)(((nuint)pBuffer + SizeOfVector128) & ~(nuint)MaskOfAllBitsInVector128);

#if DEBUG
        long numBytesRead = pBuffer - pOriginalBuffer;
        Debug.Assert(0 < numBytesRead && numBytesRead <= SizeOfVector128, "We should've made forward progress of at least one byte.");
        Debug.Assert((nuint)numBytesRead <= bufferLength, "We shouldn't have read past the end of the input buffer.");
#endif

        // Adjust the remaining length to account for what we just read.

        bufferLength += (nuint)pOriginalBuffer;
        bufferLength -= (nuint)pBuffer;

        // The buffer is now properly aligned.
        // Read 2 vectors at a time if possible.

        if (bufferLength >= 2 * SizeOfVector128)
        {
            byte* pFinalVectorReadPos = (byte*)((nuint)pBuffer + bufferLength - 2 * SizeOfVector128);

            // After this point, we no longer need to update the bufferLength value.

            do
            {
                if (Sse2.IsSupported)
                {
                    Vector128<byte> firstVector = Sse2.LoadAlignedVector128(pBuffer);
                    Vector128<byte> secondVector = Sse2.LoadAlignedVector128(pBuffer + SizeOfVector128);

                    currentSseMask = (uint)Sse2.MoveMask(firstVector);
                    secondSseMask = (uint)Sse2.MoveMask(secondVector);
                    if (ContainsNonAsciiByte_Sse2(currentSseMask | secondSseMask))
                    {
                        goto FoundNonAsciiDataInInnerLoop;
                    }
                }
                else if (AdvSimd.Arm64.IsSupported)
                {
                    Vector128<byte> firstVector = AdvSimd.LoadVector128(pBuffer);
                    Vector128<byte> secondVector = AdvSimd.LoadVector128(pBuffer + SizeOfVector128);

                    currentAdvSimdIndex = (uint)GetIndexOfFirstNonAsciiByteInLane_AdvSimd(firstVector, bitmask);
                    secondAdvSimdIndex = (uint)GetIndexOfFirstNonAsciiByteInLane_AdvSimd(secondVector, bitmask);
                    if (ContainsNonAsciiByte_AdvSimd(currentAdvSimdIndex) || ContainsNonAsciiByte_AdvSimd(secondAdvSimdIndex))
                    {
                        goto FoundNonAsciiDataInInnerLoop;
                    }
                }
                else
                {
                    throw new PlatformNotSupportedException();
                }

                pBuffer += 2 * SizeOfVector128;
            } while (pBuffer <= pFinalVectorReadPos);
        }

        // We have somewhere between 0 and (2 * vector length) - 1 bytes remaining to read from.
        // Since the above loop doesn't update bufferLength, we can't rely on its absolute value.
        // But we _can_ rely on it to tell us how much remaining data must be drained by looking
        // at what bits of it are set. This works because had we updated it within the loop above,
        // we would've been adding 2 * SizeOfVector128 on each iteration, but we only care about
        // bits which are less significant than those that the addition would've acted on.

        // If there is fewer than one vector length remaining, skip the next aligned read.

        if ((bufferLength & SizeOfVector128) == 0)
        {
            goto DoFinalUnalignedVectorRead;
        }

        // At least one full vector's worth of data remains, so we can safely read it.
        // Remember, at this point pBuffer is still aligned.

        if (Sse2.IsSupported)
        {
            currentSseMask = (uint)Sse2.MoveMask(Sse2.LoadAlignedVector128(pBuffer));
            if (ContainsNonAsciiByte_Sse2(currentSseMask))
            {
                goto FoundNonAsciiDataInCurrentChunk;
            }
        }
        else if (AdvSimd.Arm64.IsSupported)
        {
            currentAdvSimdIndex = (uint)GetIndexOfFirstNonAsciiByteInLane_AdvSimd(AdvSimd.LoadVector128(pBuffer), bitmask);
            if (ContainsNonAsciiByte_AdvSimd(currentAdvSimdIndex))
            {
                goto FoundNonAsciiDataInCurrentChunk;
            }
        }
        else
        {
            throw new PlatformNotSupportedException();
        }

    IncrementCurrentOffsetBeforeFinalUnalignedVectorRead:

        pBuffer += SizeOfVector128;

    DoFinalUnalignedVectorRead:

        if (((byte)bufferLength & MaskOfAllBitsInVector128) != 0)
        {
            // Perform an unaligned read of the last vector.
            // We need to adjust the pointer because we're re-reading data.

            pBuffer += (bufferLength & MaskOfAllBitsInVector128) - SizeOfVector128;

            if (Sse2.IsSupported)
            {
                currentSseMask = (uint)Sse2.MoveMask(Sse2.LoadVector128(pBuffer)); // unaligned load
                if (ContainsNonAsciiByte_Sse2(currentSseMask))
                {
                    goto FoundNonAsciiDataInCurrentChunk;
                }
            }
            else if (AdvSimd.Arm64.IsSupported)
            {
                currentAdvSimdIndex = (uint)GetIndexOfFirstNonAsciiByteInLane_AdvSimd(AdvSimd.LoadVector128(pBuffer), bitmask); // unaligned load
                if (ContainsNonAsciiByte_AdvSimd(currentAdvSimdIndex))
                {
                    goto FoundNonAsciiDataInCurrentChunk;
                }
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            pBuffer += SizeOfVector128;
        }

    Finish:
        return (nuint)pBuffer - (nuint)pOriginalBuffer; // and we're done!

    FoundNonAsciiDataInInnerLoop:

        // If the current (first) mask isn't the mask that contains non-ASCII data, then it must
        // instead be the second mask. If so, skip the entire first mask and drain ASCII bytes
        // from the second mask.

        if (Sse2.IsSupported)
        {
            if (!ContainsNonAsciiByte_Sse2(currentSseMask))
            {
                pBuffer += SizeOfVector128;
                currentSseMask = secondSseMask;
            }
        }
        else if (AdvSimd.IsSupported)
        {
            if (!ContainsNonAsciiByte_AdvSimd(currentAdvSimdIndex))
            {
                pBuffer += SizeOfVector128;
                currentAdvSimdIndex = secondAdvSimdIndex;
            }
        }
        else
        {
            throw new PlatformNotSupportedException();
        }
    FoundNonAsciiDataInCurrentChunk:

        if (Sse2.IsSupported)
        {
            // The mask contains - from the LSB - a 0 for each ASCII byte we saw, and a 1 for each non-ASCII byte.
            // Tzcnt is the correct operation to count the number of zero bits quickly. If this instruction isn't
            // available, we'll fall back to a normal loop.
            Debug.Assert(ContainsNonAsciiByte_Sse2(currentSseMask), "Shouldn't be here unless we see non-ASCII data.");
            pBuffer += (uint)BitOperations.TrailingZeroCount(currentSseMask);
        }
        else if (AdvSimd.Arm64.IsSupported)
        {
            Debug.Assert(ContainsNonAsciiByte_AdvSimd(currentAdvSimdIndex), "Shouldn't be here unless we see non-ASCII data.");
            pBuffer += currentAdvSimdIndex;
        }
        else
        {
            throw new PlatformNotSupportedException();
        }

        goto Finish;

    FoundNonAsciiDataInCurrentDWord:

        uint currentDWord;
        Debug.Assert(!AllBytesInUInt32AreAscii(currentDWord), "Shouldn't be here unless we see non-ASCII data.");
        pBuffer += CountNumberOfLeadingAsciiBytesFromUInt32WithSomeNonAsciiData(currentDWord);

        goto Finish;

    InputBufferLessThanOneVectorInLength:

        // These code paths get hit if the original input length was less than one vector in size.
        // We can't perform vectorized reads at this point, so we'll fall back to reading primitives
        // directly. Note that all of these reads are unaligned.

        Debug.Assert(bufferLength < SizeOfVector128);

        // QWORD drain

        if ((bufferLength & 8) != 0)
        {
            if (UIntPtr.Size == sizeof(ulong))
            {
                // If we can use 64-bit tzcnt to count the number of leading ASCII bytes, prefer it.

                ulong candidateUInt64 = Unsafe.ReadUnaligned<ulong>(pBuffer);
                if (!AllBytesInUInt64AreAscii(candidateUInt64))
                {
                    // Clear everything but the high bit of each byte, then tzcnt.
                    // Remember to divide by 8 at the end to convert bit count to byte count.

                    candidateUInt64 &= UInt64HighBitsOnlyMask;
                    pBuffer += (nuint)(BitOperations.TrailingZeroCount(candidateUInt64) >> 3);
                    goto Finish;
                }
            }
            else
            {
                // If we can't use 64-bit tzcnt, no worries. We'll just do 2x 32-bit reads instead.

                currentDWord = Unsafe.ReadUnaligned<uint>(pBuffer);
                uint nextDWord = Unsafe.ReadUnaligned<uint>(pBuffer + 4);

                if (!AllBytesInUInt32AreAscii(currentDWord | nextDWord))
                {
                    // At least one of the values wasn't all-ASCII.
                    // We need to figure out which one it was and stick it in the currentMask local.

                    if (AllBytesInUInt32AreAscii(currentDWord))
                    {
                        currentDWord = nextDWord; // this one is the culprit
                        pBuffer += 4;
                    }

                    goto FoundNonAsciiDataInCurrentDWord;
                }
            }

            pBuffer += 8; // successfully consumed 8 ASCII bytes
        }

        // DWORD drain

        if ((bufferLength & 4) != 0)
        {
            currentDWord = Unsafe.ReadUnaligned<uint>(pBuffer);

            if (!AllBytesInUInt32AreAscii(currentDWord))
            {
                goto FoundNonAsciiDataInCurrentDWord;
            }

            pBuffer += 4; // successfully consumed 4 ASCII bytes
        }

        // WORD drain
        // (We movzx to a DWORD for ease of manipulation.)

        if ((bufferLength & 2) != 0)
        {
            currentDWord = Unsafe.ReadUnaligned<ushort>(pBuffer);

            if (!AllBytesInUInt32AreAscii(currentDWord))
            {
                // We only care about the 0x0080 bit of the value. If it's not set, then we
                // increment currentOffset by 1. If it's set, we don't increment it at all.

                pBuffer += (nuint)((nint)(sbyte)currentDWord >> 7) + 1;
                goto Finish;
            }

            pBuffer += 2; // successfully consumed 2 ASCII bytes
        }

        // BYTE drain

        if ((bufferLength & 1) != 0)
        {
            // sbyte has non-negative value if byte is ASCII.

            if (*(sbyte*)(pBuffer) >= 0)
            {
                pBuffer++; // successfully consumed a single byte
            }
        }

        goto Finish;
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
            Vector128<byte> maxBytes = AdvSimd.Arm64.MaxPairwise(asciiVector, asciiVector);
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
                Vector128<ushort> asciiMaskForTestZ = Vector128.Create((ushort)0xFF80);
                // If a non-ASCII bit is set in any WORD of the vector, we have seen non-ASCII data.
                return !Sse41.TestZ(utf16Vector.AsInt16(), asciiMaskForTestZ.AsInt16());
            }
            else
            {
                Vector128<ushort> asciiMaskForAddSaturate = Vector128.Create((ushort)0x7F80);
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
            Vector128<ushort> maxChars = AdvSimd.Arm64.MaxPairwise(utf16Vector, utf16Vector);
            return (maxChars.AsUInt64().ToScalar() & 0xFF80FF80FF80FF80) != 0;
        }
        else
        {
            const ushort asciiMask = ushort.MaxValue - 127; // 0xFF80
            Vector128<ushort> zeroIsAscii = utf16Vector & Vector128.Create(asciiMask);
            // If a non-ASCII bit is set in any WORD of the vector, we have seen non-ASCII data.
            return zeroIsAscii != Vector128<ushort>.Zero;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool VectorContainsNonAsciiChar(Vector256<ushort> utf16Vector)
    {
        if (Avx.IsSupported)
        {
            Vector256<ushort> asciiMaskForTestZ = Vector256.Create((ushort)0xFF80);
            return !Avx.TestZ(utf16Vector.AsInt16(), asciiMaskForTestZ.AsInt16());
        }
        else
        {
            const ushort asciiMask = ushort.MaxValue - 127; // 0xFF80
            Vector256<ushort> zeroIsAscii = utf16Vector & Vector256.Create(asciiMask);
            // If a non-ASCII bit is set in any WORD of the vector, we have seen non-ASCII data.
            return zeroIsAscii != Vector256<ushort>.Zero;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool VectorContainsNonAsciiChar(Vector512<ushort> utf16Vector)
    {
        const ushort asciiMask = ushort.MaxValue - 127; // 0xFF80
        Vector512<ushort> zeroIsAscii = utf16Vector & Vector512.Create(asciiMask);
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
