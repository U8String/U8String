using System.Diagnostics;

namespace U8Primitives;

public readonly struct U8Range : IEquatable<U8Range>
{
    internal readonly int Offset;
    public readonly int Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8Range(int offset, int length)
    {
        Debug.Assert((uint)offset <= int.MaxValue);
        Debug.Assert((uint)length <= int.MaxValue);

        Offset = offset;
        Length = length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8Range Slice(U8String value, int start)
    {
        Debug.Assert((uint)start <= int.MaxValue);

        return new(value.Offset + start, value.Length - start);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static U8Range Slice(U8String value, int start, int length)
    {
        Debug.Assert((uint)start <= int.MaxValue);
        Debug.Assert((uint)length <= int.MaxValue);

        return new(value.Offset + start, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(U8Range other)
    {
        var (offset, length) = (Offset, Length);
        var (otherOffset, otherLength) = (other.Offset, other.Length);

        return offset == otherOffset && length == otherLength;
    }

    public override bool Equals(object? obj)
    {
        return obj is U8Range range && Equals(range);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Offset, Length);
    }

    public static bool operator ==(U8Range left, U8Range right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(U8Range left, U8Range right)
    {
        return !(left == right);
    }
}
