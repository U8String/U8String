using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace U8.InteropServices;

[EditorBrowsable(EditorBrowsableState.Advanced)]
[CustomMarshaller(typeof(U8String), MarshalMode.Default, typeof(U8StringMarshalling))]
[CustomMarshaller(typeof(U8String), MarshalMode.ManagedToUnmanagedIn, typeof(FullCStr))]
public static unsafe class U8StringMarshalling
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String ConvertToManaged(byte* unmanaged)
    {
        return new U8String(unmanaged);
    }

    [CustomMarshaller(typeof(U8String), MarshalMode.ManagedToUnmanagedIn, typeof(LightweightCStr))]
    public static class LightweightCStr
    {
        // TODO: see if there's a way to create this on stack on occasion
        // the callee writes to the address, messing up the null byte
        static byte Empty = (byte)'\0';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte* ConvertToUnmanaged(U8String managed)
        {
            return (byte*)Unsafe.AsPointer(
                ref !managed.IsEmpty ? ref managed.UnsafeRef : ref Empty);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly byte GetPinnableReference(U8String managed)
        {
            if (managed.IsNullTerminated)
            {
                return ref managed.UnsafeRef;
            }
            else if (managed.IsEmpty)
            {
                return ref Empty;
            }

            return ref NullTerminate(managed);

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
                    ptr = ref Empty;
                }

                return ref ptr;
            }
        }
    }

    [CustomMarshaller(typeof(U8String), MarshalMode.ManagedToUnmanagedIn, typeof(FullCStr))]
    public ref struct FullCStr
    {
        static byte Empty = (byte)'\0';

        ref byte _ptr;
        bool _allocated;

        public static int BufferSize => 256;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FromManaged(U8String managed, Span<byte> buffer)
        {
            if (buffer.Length < BufferSize)
            {
                ThrowHelpers.DestinationTooShort();
            }

            _allocated = false;

            if (managed.IsNullTerminated)
            {
                _ptr = ref managed.UnsafeRef;
            }
            else if (managed.IsEmpty)
            {
                _ptr = ref Empty;
            }
            else
            {
                NullTerminate(
                    ref managed.UnsafeRef,
                    ref buffer.AsRef(),
                    managed.Length + 1);
            }
        }

        void NullTerminate(ref byte src, [UnscopedRef] ref byte dst, int length)
        {
            if (length > BufferSize)
            {
                dst = ref Unsafe.AsRef<byte>((byte*)NativeMemory.Alloc((uint)length));
                _allocated = true;
            }

            MemoryMarshal.CreateSpan(ref src, length).CopyToUnsafe(ref dst);
            dst.Add(length - 1) = 0;

            _ptr = ref dst;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref readonly byte GetPinnableReference() => ref _ptr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly byte* ToUnmanaged() => (byte*)Unsafe.AsPointer(ref _ptr);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        static byte Empty = (byte)'\0';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte* ConvertToUnmanaged(U8String managed)
        {
            return (byte*)Unsafe.AsPointer(
                ref !managed.IsEmpty ? ref managed.UnsafeRef : ref Empty);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly byte GetPinnableReference(U8String managed)
        {
            return ref !managed.IsEmpty ? ref managed.UnsafeRef : ref Empty;
        }
    }

    public static byte* ConvertToUnmanaged(U8String managed)
    {
        throw new NotSupportedException(
            "Use one of the member types for explicitly specified marshalling: " +
            $"{nameof(FullCStr)}, {nameof(LightweightCStr)} or {nameof(AssumeNullTerminated)}.");
    }
}
