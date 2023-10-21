namespace U8Primitives;

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
    public U8String Slice(U8Range range)
    {
        var source = Value;
        if (source is null && range.Length != 0)
        {
            ThrowHelpers.ArgumentOutOfRange();
        }

        if (range.Length > 0)
        {
            var (offset, length) = (range.Offset, range.Length);
            if ((ulong)(uint)offset + (ulong)(uint)length > (ulong)(uint)source!.Length)
            {
                ThrowHelpers.ArgumentOutOfRange();
            }

            if (U8Info.IsContinuationByte(source.AsRef(offset)) || (
                length < source.Length && U8Info.IsContinuationByte(source.AsRef(offset + length))))
            {
                ThrowHelpers.ArgumentOutOfRange();
            }

            return new(source, range);
        }

        return default;
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
