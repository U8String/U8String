using System.Diagnostics;

namespace U8Primitives;

internal static class U8Literals
{
    internal static class Int32
    {
        static readonly U8String[] Numbers =
        [
            new U8String("-1\0"u8, skipValidation: true),
            new U8String("0\0"u8, skipValidation: true),
            new U8String("1\0"u8, skipValidation: true),
            new U8String("2\0"u8, skipValidation: true),
            new U8String("3\0"u8, skipValidation: true),
            new U8String("4\0"u8, skipValidation: true),
            new U8String("5\0"u8, skipValidation: true),
            new U8String("6\0"u8, skipValidation: true),
            new U8String("7\0"u8, skipValidation: true),
            new U8String("8\0"u8, skipValidation: true),
            new U8String("9\0"u8, skipValidation: true),
            new U8String("10\0"u8, skipValidation: true),
            new U8String("11\0"u8, skipValidation: true),
            new U8String("12\0"u8, skipValidation: true),
            new U8String("13\0"u8, skipValidation: true),
            new U8String("14\0"u8, skipValidation: true),
            new U8String("15\0"u8, skipValidation: true),
            new U8String("16\0"u8, skipValidation: true),
            new U8String("17\0"u8, skipValidation: true),
            new U8String("18\0"u8, skipValidation: true),
            new U8String("19\0"u8, skipValidation: true),
            new U8String("20\0"u8, skipValidation: true),
            new U8String("21\0"u8, skipValidation: true),
            new U8String("22\0"u8, skipValidation: true),
            new U8String("23\0"u8, skipValidation: true),
            new U8String("24\0"u8, skipValidation: true),
            new U8String("25\0"u8, skipValidation: true),
            new U8String("26\0"u8, skipValidation: true),
            new U8String("27\0"u8, skipValidation: true),
            new U8String("28\0"u8, skipValidation: true),
            new U8String("29\0"u8, skipValidation: true),
            new U8String("30\0"u8, skipValidation: true),
            new U8String("31\0"u8, skipValidation: true),
            new U8String("32\0"u8, skipValidation: true)
        ];

        internal static bool TryGet(int value, out U8String literal)
        {
            const int lowerBoundInclusive = -1;
            const int upperBoundInclusive = 32;

            if (value is >= lowerBoundInclusive and <= upperBoundInclusive)
            {
                literal = Numbers.AsRef(value + 1);
                return true;
            }

            Unsafe.SkipInit(out literal);
            return false;
        }
    }

    internal static class Int64
    {
        static readonly U8String[] Numbers =
        [
            new U8String("-1\0"u8, skipValidation: true),
            new U8String("0\0"u8, skipValidation: true),
            new U8String("1\0"u8, skipValidation: true),
            new U8String("2\0"u8, skipValidation: true),
            new U8String("3\0"u8, skipValidation: true),
            new U8String("4\0"u8, skipValidation: true),
            new U8String("5\0"u8, skipValidation: true),
            new U8String("6\0"u8, skipValidation: true),
            new U8String("7\0"u8, skipValidation: true),
            new U8String("8\0"u8, skipValidation: true),
            new U8String("9\0"u8, skipValidation: true),
            new U8String("10\0"u8, skipValidation: true),
            new U8String("11\0"u8, skipValidation: true),
            new U8String("12\0"u8, skipValidation: true),
            new U8String("13\0"u8, skipValidation: true),
            new U8String("14\0"u8, skipValidation: true),
            new U8String("15\0"u8, skipValidation: true),
            new U8String("16\0"u8, skipValidation: true),
            new U8String("17\0"u8, skipValidation: true),
            new U8String("18\0"u8, skipValidation: true),
            new U8String("19\0"u8, skipValidation: true),
            new U8String("20\0"u8, skipValidation: true),
            new U8String("21\0"u8, skipValidation: true),
            new U8String("22\0"u8, skipValidation: true),
            new U8String("23\0"u8, skipValidation: true),
            new U8String("24\0"u8, skipValidation: true),
            new U8String("25\0"u8, skipValidation: true),
            new U8String("26\0"u8, skipValidation: true),
            new U8String("27\0"u8, skipValidation: true),
            new U8String("28\0"u8, skipValidation: true),
            new U8String("29\0"u8, skipValidation: true),
            new U8String("30\0"u8, skipValidation: true),
            new U8String("31\0"u8, skipValidation: true),
            new U8String("32\0"u8, skipValidation: true)
        ];

        internal static bool TryGet(long value, out U8String literal)
        {
            const long lowerBoundInclusive = -1;
            const long upperBoundInclusive = 32;

            if (value is >= lowerBoundInclusive and <= upperBoundInclusive)
            {
                literal = Numbers.AsRef((int)(value + 1));
                return true;
            }

            Unsafe.SkipInit(out literal);
            return false;
        }
    }
}
