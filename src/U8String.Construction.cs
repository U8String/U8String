using System.Diagnostics;

namespace U8Primitives;

public readonly partial struct U8String
{
    public U8String(ReadOnlySpan<byte> value)
    {
        if (!value.IsEmpty)
        {
            Validate(value);
            _value = value.ToArray();
            _offset = 0;
            _length = (uint)_value.Length;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8String(byte[]? value, uint offset, uint length)
    {
        _value = value;
        _offset = offset;
        _length = length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal U8String(ReadOnlySpan<byte> value, bool skipValidation)
    {
        Debug.Assert(skipValidation);

        _value = value.ToArray();
        _offset = 0;
        _length = (uint)_value.Length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U8String(string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            this = Parse(value.AsSpan(), null);
        }
    }

    // Tracks https://github.com/dotnet/runtime/issues/87569
    public static U8String Create(/*params*/ ReadOnlySpan<byte> items) => new(items);

    public object Clone() => new U8String(AsSpan(), skipValidation: true);
}
