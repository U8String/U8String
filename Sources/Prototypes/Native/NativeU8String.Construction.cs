using System.Diagnostics;
using System.Runtime.InteropServices;

namespace U8.InteropServices;

internal static partial class NativeU8String
{
    public static NativeU8String<DefaultAllocator> Create(ReadOnlySpan<byte> value)
    {
        return Create<DefaultAllocator>(value);
    }

    public static NativeU8String<T> Create<T>(ReadOnlySpan<byte> value)
        where T : struct, IU8Allocator
    {
        return new(value);
    }

    public static NativeU8String<DefaultAllocator> CreateUnchecked(ReadOnlySpan<byte> value)
    {
        return CreateUnchecked<DefaultAllocator>(value);
    }

    public static NativeU8String<T> CreateUnchecked<T>(ReadOnlySpan<byte> value)
        where T : struct, IU8Allocator
    {
        return new(value, skipValidation: true);
    }
}

internal unsafe readonly partial struct NativeU8String<T>
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
                _ptr = T.Alloc((uint)value.Length + 1);
                _ptr[value.Length] = (byte)'\0';
            }
            else
            {
                // TODO: Should we trim the null terminator here as well?
                _ptr = T.Alloc((uint)value.Length);
            }
            _length = (nint)(uint)value.Length;

            value.CopyTo(MemoryMarshal.CreateSpan(ref _ptr[0], value.Length));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal NativeU8String(byte* ptr, nint length)
    {
        _ptr = ptr;
        _length = length;
    }
}
