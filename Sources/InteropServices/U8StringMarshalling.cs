using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace U8.InteropServices;

[CustomMarshaller(typeof(U8String), MarshalMode.ManagedToUnmanagedIn, typeof(FullCStr))]
public static unsafe class U8StringMarshalling
{
    static readonly byte[] Empty = new byte[1];

    [CustomMarshaller(typeof(U8String), MarshalMode.ManagedToUnmanagedIn, typeof(LightweightCStr))]
    public static class LightweightCStr
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte* ConvertToUnmanaged(U8String managed)
        {
            var ptr = (byte*)null;
            if (!managed.IsEmpty)
            {
                ptr = (byte*)Unsafe.AsPointer(ref managed.UnsafeRef);
            }

            return ptr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly byte GetPinnableReference(U8String managed)
        {
            if (managed.IsNullTerminated)
            {
                return ref managed.UnsafeRef;
            }

            return ref NullTerminate(managed);

            [MethodImpl(MethodImplOptions.NoInlining)]
            static ref byte NullTerminate(U8String managed)
            {
                ref var ptr = ref Unsafe.NullRef<byte>();

                if (!managed.IsEmpty)
                {
                    var length = managed.Length;
                    ptr = ref new byte[(nint)(uint)(length + 1)].AsRef();

                    managed.UnsafeSpan.CopyToUnsafe(ref ptr);
                }
                else
                {
                    ptr = ref Empty.AsRef();
                }

                return ref ptr;
            }
        }
    }

    [CustomMarshaller(typeof(U8String), MarshalMode.ManagedToUnmanagedIn, typeof(FullCStr))]
    public ref struct FullCStr
    {
        InlineBuffer128 _buffer;
        ref byte _ptr;
        bool _allocated;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FullCStr()
        {
            Unsafe.SkipInit(out _buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FromManaged(U8String managed)
        {
            if (managed.IsNullTerminated)
            {
                _ptr = ref managed.UnsafeRef;
            }
            else
            {
                NullTerminate(managed);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        void NullTerminate(U8String managed)
        {
            if (!managed.IsEmpty)
            {
                ref var dst = ref Unsafe.NullRef<byte>();
                var length = (nuint)(uint)managed.Length + 1;

                if (length <= InlineBuffer128.Length)
                {
                    dst = ref _ptr = ref _buffer.AsSpan().AsRef();
                }
                else
                {
                    dst = ref _ptr = ref Unsafe.AsRef<byte>((byte*)NativeMemory.Alloc(length));
                    _allocated = true;
                }

                managed.UnsafeSpan.CopyToUnsafe(ref dst);
                dst.Add((int)length - 1) = 0;
            }
            else
            {
                _ptr = ref Empty.AsRef();
            }
        }

        public readonly ref readonly byte GetPinnableReference() => ref _ptr;

        public readonly byte* ToUnmanaged() => (byte*)Unsafe.AsPointer(ref _ptr);

        public readonly void Free()
        {
            if (_allocated)
            {
                NativeMemory.Free(Unsafe.AsPointer(ref _ptr));
            }
        }
    }

    [CustomMarshaller(typeof(U8String), MarshalMode.ManagedToUnmanagedIn, typeof(AssumeNullTerminated))]
    public static class AssumeNullTerminated
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte* ConvertToUnmanaged(U8String managed)
        {
            return (byte*)Unsafe.AsPointer(
                ref !managed.IsEmpty ? ref managed.UnsafeRef : ref Empty.AsRef());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly byte GetPinnableReference(U8String managed)
        {
            return ref !managed.IsEmpty ? ref managed.UnsafeRef : ref Empty.AsRef();
        }
    }
}
