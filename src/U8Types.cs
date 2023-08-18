#pragma warning disable RCS1237 // Use bit-shift operator. Why: no.
namespace U8Primitives;

[Flags]
public enum U8SplitOptions : byte
{
    None = 0,
    RemoveEmpty = 1,
    Trim = 2,
}

internal enum U8Size : ushort
{
    Ascii = 1,
    Two = 2,
    Three = 3,
    Four = 4
}
