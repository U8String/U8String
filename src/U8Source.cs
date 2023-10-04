namespace U8Primitives;

public readonly struct U8Source
{
    internal readonly byte[]? Value;

    public U8Source(U8String value)
    {
        Value = value._value;
    }

    internal U8Source(byte[]? value)
    {
        Value = value;
    }

    public U8String Slice(U8Range range)
    {
        var source = Value;
        if (source is null && range.Length != 0)
        {
            ThrowHelpers.ArgumentOutOfRange(nameof(range));
        }

        if (range.Length > 0)
        {
            var end = range.Offset + range.Length;
            if ((uint)end > (uint)source!.Length)
            {
                ThrowHelpers.ArgumentOutOfRange(nameof(range));
            }

            if (U8Info.IsContinuationByte(in source.AsRef(range.Offset))
                || ((uint)end < (uint)source.Length && U8Info.IsContinuationByte(in source.AsRef(end))))
            {
                // TODO: Exception message UX
                ThrowHelpers.InvalidSplit();
            }

            return new(source, range);
        }

        return default;
    }
}
