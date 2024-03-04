namespace U8;

readonly struct ByteArray(byte[] array)
{
    internal readonly byte[] Array = array;

    public static implicit operator byte[](ByteArray value) => value.Array;
    public static implicit operator ByteArray(byte[] value) => new(value);
}