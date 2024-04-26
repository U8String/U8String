using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace U8.InteropServices;

// TODO: Consider switching to stateful structs
// TODO: Double-check that the flow of get pinnable ref -> ConvertToUnmanaged is correct
[EditorBrowsable(EditorBrowsableState.Advanced)]
[CustomMarshaller(typeof(U8String), MarshalMode.ManagedToUnmanagedIn, typeof(Raw))]
[CustomMarshaller(typeof(U8String), MarshalMode.Default, typeof(U8StringMarshalling))]
public static unsafe class U8StringMarshalling
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8String ConvertToManaged(byte* unmanaged)
    {
        return new U8String(unmanaged);
    }

    public static byte* ConvertToUnmanaged(U8String managed)
    {
        throw new NotSupportedException(
            "Use one of the member types for explicitly specified marshalling: " +
            $"{nameof(Raw)}, {nameof(LikelyNullTerminated)} or {nameof(UnlikelyNullTerminated)}.");
    }

    [CustomMarshaller(typeof(U8String), MarshalMode.ManagedToUnmanagedIn, typeof(LikelyNullTerminated))]
    public static class LikelyNullTerminated
    {
        // TODO: see if there's a way to create this on stack on occasion
        // the callee writes to the address, messing up the null byte
        static byte Empty = (byte)'\0';

        public static byte* ConvertToUnmanaged(U8String managed)
        {
            throw new NotSupportedException($"""
                It is expected for the generated marshalling code to call {nameof(GetPinnableReference)} instead.");
                If you are seeing this exception, please file an issue at https://github.com/U8String/U8String/issues/new
                """);
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
                ref var ptr = ref new byte[(nint)(uint)(managed.Length + 1)].AsRef();

                managed.UnsafeSpan.CopyToUnsafe(ref ptr);
                return ref ptr;
            }
        }
    }

    [CustomMarshaller(typeof(U8String), MarshalMode.ManagedToUnmanagedIn, typeof(UnlikelyNullTerminated))]
    public ref struct UnlikelyNullTerminated
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
                NullTerminateCore(
                    ref managed.UnsafeRef,
                    ref buffer.AsRef(),
                    managed.Length);
            }
        }

        void NullTerminateCore(ref byte src, [UnscopedRef] ref byte dst, int length)
        {
            if (length > BufferSize)
            {
                dst = ref Unsafe.AsRef<byte>((byte*)NativeMemory.Alloc((uint)length + 1));
                _allocated = true;
            }

            MemoryMarshal.CreateSpan(ref src, length).CopyToUnsafe(ref dst);
            dst.Add(length) = 0;

            _ptr = ref dst;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref readonly byte GetPinnableReference() => ref _ptr;

        // SAFETY: _ptr is always pinned and returned pointer is expected not to
        // outlive the duration of the p/invoke call it is passed to.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly byte* ToUnmanaged() => (byte*)Unsafe.AsPointer(ref _ptr);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Free()
        {
            if (_allocated)
            {
                // SAFETY: _allocated _ptr originates from NativeMemory.Alloc
                NativeMemory.Free(Unsafe.AsPointer(ref _ptr));
            }
        }
    }

    [CustomMarshaller(typeof(U8String), MarshalMode.ManagedToUnmanagedIn, typeof(Raw))]
    public static class Raw
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte* ConvertToUnmanaged(U8String managed)
        {
            throw new NotSupportedException($"""
                It is expected for the generated marshalling code to call {nameof(GetPinnableReference)} instead.
                If you are seeing this exception, please file an issue at https://github.com/U8String/U8String/issues/new
                """);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref readonly byte GetPinnableReference(U8String managed)
        {
            return ref managed.GetPinnableReference();
        }
    }
}