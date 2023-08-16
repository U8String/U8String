using System.Numerics;

namespace U8Primitives;

#pragma warning disable RCS1001, CS8500
internal static class U8Unsafe
{
    internal static unsafe void BroadcastReference(Span<U8String> destination, byte[] reference)
    {
        // Casts destination to a byte*, broadcasts the reference to every other nuint element,
        // and then casts back to a U8String*.

        fixed (byte* _ = reference)
        fixed (U8String* ptr = destination)
        {
            var val = *(nuint*)&reference;
            var vec = new Vector<nuint>(val);

            throw new NotImplementedException();
        }
    }
}
