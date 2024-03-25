namespace U8.Tests;

public partial class U8EnumTests
{
    enum ByteEnum : byte
    {
        Zero = 0, One = 1, Two = 2, Three = 3, Four = 4, Five = 5,
        Six = 6, Seven = 7, Eight = 8, Nine = 9, Ten = 10, Eleven = 11,
    }

    enum SByteEnum : sbyte
    {
        MinusFive = -5, MinusFour = -4, MinusThree = -3, MinusTwo = -2, MinusOne = -1,
        Zero = 0, One = 1, Two = 2, Three = 3, Four = 4, Five = 5,
    }

    enum ShortEnum : short
    {
        MinusFive = -5, MinusFour = -4, MinusThree = -3, MinusTwo = -2, MinusOne = -1,
        Zero = 0, One = 1, Two = 2, Three = 3, Four = 4, Five = 5,
    }

    enum UShortEnum : ushort
    {
        Zero = 0, One = 1, Two = 2, Three = 3, Four = 4, Five = 5,
        Six = 6, Seven = 7, Eight = 8, Nine = 9, Ten = 10, Eleven = 11,
    }

    enum IntEnum : int
    {
        MinusFive = -5, MinusFour = -4, MinusThree = -3, MinusTwo = -2, MinusOne = -1,
        Zero = 0, One = 1, Two = 2, Three = 3, Four = 4, Five = 5,
    }

    enum UIntEnum : uint
    {
        Zero = 0, One = 1, Two = 2, Three = 3, Four = 4, Five = 5,
        Six = 6, Seven = 7, Eight = 8, Nine = 9, Ten = 10, Eleven = 11,
    }

    enum LongEnum : long
    {
        MinusFive = -5, MinusFour = -4, MinusThree = -3, MinusTwo = -2, MinusOne = -1,
        Zero = 0, One = 1, Two = 2, Three = 3, Four = 4, Five = 5,
    }

    enum ULongEnum : ulong
    {
        Zero = 0, One = 1, Two = 2, Three = 3, Four = 4, Five = 5,
        Six = 6, Seven = 7, Eight = 8, Nine = 9, Ten = 10, Eleven = 11,
    }

    enum NonContiguousEnum
    {
        MinusFive = -5, MinusThree = -3, MinusOne = -1, One = 1, Three = 3, Five = 5,
        Zero = 0, Two = 2, Four = 4, Six = 6, Eight = 8, Ten = 10,
        Twelve = 12, Fourteen = 14, Sixteen = 16, Eighteen = 18, Twenty = 20,
    }
}