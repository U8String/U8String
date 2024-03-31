using System.Text;

using U8.Abstractions;
using U8.Shared;

namespace U8.Comparison;

// Should this be called InvariantIgnoreCase? There is ambiguity for variable-length
// case folding. What should be the policy for 54 characters that are affected by this?
// TODO: Double check if there are non-ascii characters that would evaluate to ascii
// characters when case-folded. Maybe it is fine since this is ordinal and not invariant?
internal readonly struct U8OrdinalIgnoreCaseComparer : IU8Comparer
{
    public int Compare(U8String x, U8String y)
    {
        if (!x.IsEmpty)
        {
            if (!y.IsEmpty)
            {
                var left = x.UnsafeSpan;
                var right = y.UnsafeSpan;

                return Compare(left, right);
            }

            return 1;
        }

        return y.IsEmpty ? 0 : -1;
    }

    // TODO: Optimize. Likely depends on high effort multi-level LUT approach.
    // In fact, there's a chance we can put all canonical implementations in the ground,
    // outperforming them by a wide margin by statistically sampling character frequencies and/or
    // bitwise case folding patterns and implementing vectorized second level LUT based on
    // TBLn/TBXn instructions on ARM64 and shuffles/vpternlogs on x86_64.
    // With that said, even the naive implementation below is just 3 times slower than
    // built-in Windows implementation of OrdinalIgnoreCase.
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public int Compare(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
    {
        // TODO: Drain common prefix using case-insensitive ASCII comparison instead.
        var prefix = x.CommonPrefixLength(y);
        var length = Math.Min(x.Length, y.Length);

        ref var xptr = ref x.AsRef();
        ref var yptr = ref y.AsRef();

        var xoffset = prefix;
        var yoffset = prefix;

        while (U8Info.IsContinuationByte(in xptr.Add(xoffset)))
            xoffset--;

        while (U8Info.IsContinuationByte(in yptr.Add(yoffset)))
            yoffset--;

        while (xoffset < length && yoffset < length)
        {
            var xrune = U8Conversions.CodepointToRune(ref xptr.Add(xoffset), out var xlen);
            var yrune = U8Conversions.CodepointToRune(ref yptr.Add(yoffset), out var ylen);

            if (xrune != yrune)
            {
                xrune = Rune.ToUpperInvariant(xrune);
                yrune = Rune.ToUpperInvariant(yrune);

                if (xrune != yrune)
                {
                    return xrune.CompareTo(yrune);
                }
            }

            xoffset += xlen;
            yoffset += ylen;
        }

        return x.Length - xoffset - (y.Length - yoffset);
    }

    public bool Contains(ReadOnlySpan<byte> source, byte value)
    {
        if (!U8Info.IsAsciiLetter(value))
        {
            return source.Contains(value);
        }

        return source.ContainsAny(value, (byte)(value ^ 0x20));
    }

    public bool Contains(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
    {
        throw new NotImplementedException();
    }

    public int Count(ReadOnlySpan<byte> source, byte value)
    {
        if (!U8Info.IsAsciiLetter(value))
        {
            return source.Count(value);
        }

        int index;
        var count = 0;
        while (true)
        {
            index = source.IndexOfAny(value, (byte)(value ^ 0x20));
            if (index < 0) break;

            count++;
            source = source.SliceUnsafe(index + 1);
        }

        return count;
    }

    public int Count(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
    {
        throw new NotImplementedException();
    }

    public bool Equals(U8String x, U8String y)
    {
        // TODO: Disambiguate ordinal vs invariant and fix comparer name.
        if (x.Length == y.Length)
        {
            if (x.Length is 0 || (
                x.Offset == y.Offset && x.SourceEqual(y)))
            {
                return true;
            }
        }

        return EqualsCore(x, y);
    }

    public bool Equals(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
    {
        if (x.Length == y.Length)
        {
            ref var lptr = ref x.AsRef();
            ref var rptr = ref y.AsRef();

            if (Unsafe.AreSame(ref lptr, ref rptr))
            {
                return true;
            }
        }

        return EqualsCore(x, y);
    }

    /// <summary>
    /// Contract: both strings must start at non-continuation byte.
    /// </summary>
    static bool EqualsCore(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
    {
        // TODO: Drain common prefix using case-insensitive ASCII comparison instead.
        var prefix = x.CommonPrefixLength(y);

        ref var xptr = ref x.AsRef();
        ref var yptr = ref y.AsRef();

        var (xoffset, xlength) = (prefix, x.Length);
        var (yoffset, ylength) = (prefix, y.Length);

        while (U8Info.IsContinuationByte(in xptr.Add(xoffset)))
            xoffset--;

        while (U8Info.IsContinuationByte(in yptr.Add(yoffset)))
            yoffset--;

        while (xoffset < xlength)
        {
            if (yoffset >= ylength)
            {
                return false;
            }

            var xrune = U8Conversions.CodepointToRune(ref xptr.Add(xoffset), out var xlen);
            var yrune = U8Conversions.CodepointToRune(ref yptr.Add(yoffset), out var ylen);

            if (xrune != yrune)
            {
                xrune = Rune.ToUpperInvariant(xrune);
                yrune = Rune.ToUpperInvariant(yrune);

                if (xrune != yrune)
                {
                    return false;
                }
            }

            xoffset += xlen;
            yoffset += ylen;
        }

        return true;
    }

    public (int Offset, int Length) IndexOf(ReadOnlySpan<byte> source, byte value)
    {
        // Absolute pain because maybe ASCII letter can match to non-ASCII one?
        throw new NotImplementedException();
    }

    public (int Offset, int Length) IndexOf(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
    {
        // Even more pain because we need to match against two character candidate sequences
        throw new NotImplementedException();
    }

    public (int Offset, int Length) LastIndexOf(ReadOnlySpan<byte> source, byte value)
    {
        throw new NotImplementedException();
    }

    public (int Offset, int Length) LastIndexOf(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
    {
        throw new NotImplementedException();
    }

    public int GetHashCode(U8String value) => GetHashCode(value.AsSpan());

    public int GetHashCode(ReadOnlySpan<byte> value)
    {
        throw new NotImplementedException();
    }

    public bool StartsWith(ReadOnlySpan<byte> source, byte value)
    {
        throw new NotImplementedException();
    }

    public bool StartsWith(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
    {
        throw new NotImplementedException();
    }

    public bool EndsWith(ReadOnlySpan<byte> source, byte value)
    {
        throw new NotImplementedException();
    }

    public bool EndsWith(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
    {
        throw new NotImplementedException();
    }
}