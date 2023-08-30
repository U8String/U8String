using System.Diagnostics;
using System.Runtime.InteropServices;

namespace U8Primitives;

public unsafe readonly partial struct NativeU8String
{
    public NativeU8String(ReadOnlySpan<byte> value)
    {
        if (value.Length > 0)
        {
            U8String.Validate(value);
            this = new(value, skipValidation: true);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal NativeU8String(ReadOnlySpan<byte> value, bool skipValidation)
    {
        Debug.Assert(skipValidation);

        if (value.Length > 0)
        {
            if (value[^1] is not (byte)'\0')
            {
                // TODO: dedup ops and improve or just leave it as is?
                _ptr = (byte*)NativeMemory.Alloc((uint)value.Length + 1);
                _length = (nint)(uint)value.Length + 1;
                _ptr[value.Length] = (byte)'\0';
            }
            else
            {
                _ptr = (byte*)NativeMemory.Alloc((uint)value.Length);
                _length = (nint)(uint)value.Length;
            }

            value.CopyTo(MemoryMarshal.CreateSpan(ref _ptr[0], value.Length));
        }
    }

    public static NativeU8String CreateUnchecked(ReadOnlySpan<byte> value)
    {
        return new(value, skipValidation: true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static NativeU8String CreateFromPinnedUnchecked(ReadOnlySpan<byte> value)
    {
        var ptr = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(value));
        var length = (nint)(uint)value.Length;

        return new(ptr, length);
    }
}
