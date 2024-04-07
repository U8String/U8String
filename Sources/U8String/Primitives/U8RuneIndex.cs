using System.Runtime.InteropServices;
using System.Text;

namespace U8.Primitives;

[StructLayout(LayoutKind.Sequential)]
public readonly record struct U8RuneIndex : IEquatable<U8RuneIndex>
{
    public Rune Value { get; }
    public int Offset { get; }
    public int Length { get; }

    long Packed => Unsafe.As<U8RuneIndex, long>(ref Unsafe.AsRef(in this));

    public U8RuneIndex(Rune value, int offset)
    {
        var length = value.Utf8SequenceLength;

        if ((ulong)(uint)length + (ulong)(uint)offset > (ulong)(uint)int.MaxValue)
        {
            // TODO: EH UX
            ThrowHelpers.ArgumentOutOfRange();
        }

        Value = value;
        Offset = offset;
        Length = length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8RuneIndex(Rune value, int offset, int length)
    {
        Value = value;
        Offset = offset;
        Length = length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out Rune value, out int offset)
    {
        value = Value;
        offset = Offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out Rune value, out int offset, out int length)
    {
        value = Value;
        offset = Offset;
        length = Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(U8RuneIndex other)
    {
        return Packed == other.Packed;
    }

    public static implicit operator Rune(U8RuneIndex index) => index.Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        return Packed.GetHashCode();
    }
}
