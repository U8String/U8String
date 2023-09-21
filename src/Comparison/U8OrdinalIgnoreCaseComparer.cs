using System.Text;

using U8Primitives.Abstractions;

namespace U8Primitives;

// Should this be called InvariantIgnoreCase? There is ambiguity for variable-length
// case folding. What should be the policy for 54 characters that are affected by this?
// TODO: Double check if there are non-ascii characters that would evaluate to ascii
// characters when case-folded. Maybe it is fine since this is ordinal and not invariant?
public readonly struct U8OrdinalIgnoreCaseComparer :
    IU8Comparer,
    IU8EqualityComparer,
    IU8ContainsOperator,
    IU8CountOperator,
    IU8IndexOfOperator,
    IU8LastIndexOfOperator
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

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public int Compare(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
    {
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
        if (x.Length == y.Length)
        {
            if (x.Length != 0 && (
                x.Offset != y.Offset || !x.SourceEquals(y)))
            {
                return EqualsCore(
                    ref x.UnsafeRef, ref y.UnsafeRef, (nuint)x.Length);
            }

            return true;
        }

        return false;
    }

    public bool Equals(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
    {
        if (x.Length == y.Length)
        {
            ref var lptr = ref x.AsRef();
            ref var rptr = ref y.AsRef();

            if (!Unsafe.AreSame(ref lptr, ref rptr))
            {
                return EqualsCore(ref lptr, ref rptr, (nuint)x.Length);
            }

            return true;
        }

        return false;
    }

    static bool EqualsCore(ref byte left, ref byte right, nuint length)
    {
        throw new NotImplementedException();
    }

    public int GetHashCode(U8String value) => GetHashCode(value.AsSpan());

    public int GetHashCode(ReadOnlySpan<byte> value)
    {
        throw new NotImplementedException();
    }

    public (int Offset, int Length) IndexOf(ReadOnlySpan<byte> source, byte value)
    {
        throw new NotImplementedException();
    }

    public (int Offset, int Length) IndexOf(ReadOnlySpan<byte> source, ReadOnlySpan<byte> value)
    {
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
}