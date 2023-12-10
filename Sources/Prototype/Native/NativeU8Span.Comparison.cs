namespace U8.InteropServices;

internal unsafe readonly partial struct NativeU8Span
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(NativeU8Span other)
    {
        var deref = this;
        if (deref.Length == other.Length)
        {
            if (deref._ptr == other._ptr)
            {
                return true;
            }

            return deref.UnsafeSpan.SequenceEqual(other.UnsafeSpan);
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        return U8String.GetHashCode(this);
    }
}