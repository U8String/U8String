using System.Runtime.InteropServices;

namespace U8Primitives;

public readonly partial struct U8String
{
    public ref readonly byte this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((nint)(uint)index >= (nint)(uint)Length)
            {
                ThrowHelpers.ArgumentOutOfRange();
            }

            return ref Unsafe.Add(
                ref MemoryMarshal.GetArrayDataReference(Value!), (uint)Offset + (uint)index);
        }
    }
}
