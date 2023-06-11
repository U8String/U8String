using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace U8Primitives;

public readonly partial struct U8String :
    IEquatable<U8String>,
    IEquatable<U8String?>,
    IEquatable<byte[]>,
    ISpanParsable<U8String>,
    ISpanFormattable,
    IUtf8SpanFormattable
{
    public static readonly U8String Empty;

    private readonly byte[] _value;

    private readonly uint _offset;

    private readonly uint _length;

    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (int)_length;
    }

    public bool IsEmpty => _length is 0;

    internal ref byte FirstByte
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_value), _offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal byte IndexUnsafe(int index)
    {
        return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_value), (nint)(uint)(_offset + index));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal byte IndexUnsafe(uint index)
    {
        return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_value), _offset + index);
    }

    public bool IsAscii() => Ascii.IsValid(AsSpan());

    // TODO: Implement polyfill + wait for Utf8.IsValid to be approved
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool IsValid(ReadOnlySpan<byte> _) => true;

    [MethodImpl(MethodImplOptions.AggressiveInlining), StackTraceHidden]
    internal static void Validate(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty)
        {
            return;
        }

        if (!IsValid(value))
        {
            ThrowHelpers.MalformedUtf8Value();
        }
    }
}
