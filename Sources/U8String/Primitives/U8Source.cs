namespace U8.Primitives;

public readonly struct U8Source : IEquatable<U8Source>
{
    internal readonly byte[]? Value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8Source(U8String value)
    {
        Value = value._value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8Source(byte[]? value)
    {
        Value = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String SliceUnchecked(U8Range range)
    {
        var source = Value;
        if (range.Length > 0)
        {
            if ((ulong)(uint)range.Offset + (ulong)(uint)range.Length > (ulong)(uint)source!.Length)
            {
                ThrowHelpers.ArgumentOutOfRange();
            }
        }

        return new(source, range);
    }

    public bool Equals(U8Source other)
    {
        return ReferenceEquals(Value, other.Value);
    }

    public override bool Equals(object? obj)
    {
        if (obj is U8Source other)
        {
            return Equals(other);
        }
        else if (obj is U8String str)
        {
            return Equals(str.Source);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return Value?.GetHashCode() ?? 0;
    }

    public static bool operator ==(U8Source left, U8Source right) => left.Equals(right);
    public static bool operator ==(U8Source left, U8String right) => left.Equals(right.Source);
    public static bool operator ==(U8String left, U8Source right) => right.Equals(left.Source);

    public static bool operator !=(U8Source left, U8Source right) => !(left == right);
    public static bool operator !=(U8Source left, U8String right) => !(left == right);
    public static bool operator !=(U8String left, U8Source right) => !(left == right);
}
