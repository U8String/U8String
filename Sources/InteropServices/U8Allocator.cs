using System.Runtime.InteropServices;

namespace U8.InteropServices;

internal unsafe interface IU8Allocator
{
    static abstract byte* Alloc(nuint length);
    static abstract void Free(byte* ptr);
    static abstract byte* Realloc(byte* ptr, nuint newLength);
}

internal unsafe readonly struct DefaultAllocator : IU8Allocator
{
    public static byte* Alloc(nuint length)
    {
        return (byte*)NativeMemory.Alloc(length);
    }

    public static void Free(byte* ptr)
    {
        NativeMemory.Free(ptr);
    }

    public static byte* Realloc(byte* ptr, nuint newLength)
    {
        return (byte*)NativeMemory.Realloc(ptr, newLength);
    }
}
