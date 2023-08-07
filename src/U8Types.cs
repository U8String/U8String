using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace U8Primitives;

[Flags]
public enum U8SplitOptions : byte
{
    None = 0,
    RemoveEmpty = 1,
    Trim = 2,
}
