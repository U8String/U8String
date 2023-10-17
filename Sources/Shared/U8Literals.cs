namespace U8Primitives;

internal static class U8Literals
{
    internal static class Int32
    {
        static readonly byte[] _0 = [(byte)'0', 0];
        static readonly byte[] _1 = [(byte)'1', 0];
        static readonly byte[] _2 = [(byte)'2', 0];
        static readonly byte[] _3 = [(byte)'3', 0];
        static readonly byte[] _4 = [(byte)'4', 0];
        static readonly byte[] _5 = [(byte)'5', 0];
        static readonly byte[] _6 = [(byte)'6', 0];
        static readonly byte[] _7 = [(byte)'7', 0];
        static readonly byte[] _8 = [(byte)'8', 0];
        static readonly byte[] _9 = [(byte)'9', 0];
        static readonly byte[] _10 = [(byte)'1', (byte)'0', 0];
        static readonly byte[] _11 = [(byte)'1', (byte)'1', 0];
        static readonly byte[] _12 = [(byte)'1', (byte)'2', 0];
        static readonly byte[] _13 = [(byte)'1', (byte)'3', 0];
        static readonly byte[] _14 = [(byte)'1', (byte)'4', 0];
        static readonly byte[] _15 = [(byte)'1', (byte)'5', 0];
        static readonly byte[] _16 = [(byte)'1', (byte)'6', 0];
        static readonly byte[] _17 = [(byte)'1', (byte)'7', 0];
        static readonly byte[] _18 = [(byte)'1', (byte)'8', 0];
        static readonly byte[] _19 = [(byte)'1', (byte)'9', 0];
        static readonly byte[] _20 = [(byte)'2', (byte)'0', 0];
        static readonly byte[] _21 = [(byte)'2', (byte)'1', 0];
        static readonly byte[] _22 = [(byte)'2', (byte)'2', 0];
        static readonly byte[] _23 = [(byte)'2', (byte)'3', 0];
        static readonly byte[] _24 = [(byte)'2', (byte)'4', 0];
        static readonly byte[] _25 = [(byte)'2', (byte)'5', 0];
        static readonly byte[] _26 = [(byte)'2', (byte)'6', 0];
        static readonly byte[] _27 = [(byte)'2', (byte)'7', 0];
        static readonly byte[] _28 = [(byte)'2', (byte)'8', 0];
        static readonly byte[] _29 = [(byte)'2', (byte)'9', 0];
        static readonly byte[] _30 = [(byte)'3', (byte)'0', 0];
        static readonly byte[] _31 = [(byte)'3', (byte)'1', 0];
        static readonly byte[] _32 = [(byte)'3', (byte)'2', 0];

        internal const int UpperBoundInclusive = 32;

        internal static U8String Get(int value) => value switch
        {
            0 => new(_0, 0, 1),
            1 => new(_1, 0, 1),
            2 => new(_2, 0, 1),
            3 => new(_3, 0, 1),
            4 => new(_4, 0, 1),
            5 => new(_5, 0, 1),
            6 => new(_6, 0, 1),
            7 => new(_7, 0, 1),
            8 => new(_8, 0, 1),
            9 => new(_9, 0, 1),
            10 => new(_10, 0, 2),
            11 => new(_11, 0, 2),
            12 => new(_12, 0, 2),
            13 => new(_13, 0, 2),
            14 => new(_14, 0, 2),
            15 => new(_15, 0, 2),
            16 => new(_16, 0, 2),
            17 => new(_17, 0, 2),
            18 => new(_18, 0, 2),
            19 => new(_19, 0, 2),
            20 => new(_20, 0, 2),
            21 => new(_21, 0, 2),
            22 => new(_22, 0, 2),
            23 => new(_23, 0, 2),
            24 => new(_24, 0, 2),
            25 => new(_25, 0, 2),
            26 => new(_26, 0, 2),
            27 => new(_27, 0, 2),
            28 => new(_28, 0, 2),
            29 => new(_29, 0, 2),
            30 => new(_30, 0, 2),
            31 => new(_31, 0, 2),
            32 => new(_32, 0, 2),
            _ => ThrowHelpers.Unreachable<U8String>()
        };
    }
}