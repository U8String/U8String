// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using static U8.Shared.XxHashShared;

// Implemented from the specification at
// https://github.com/Cyan4973/xxHash/blob/f9155bd4c57e2270a4ffbb176485e5d713de1c9b/doc/xxhash_spec.md

namespace U8.Shared;

internal sealed partial class XxHash64
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong Avalanche(ulong hash)
    {
        hash ^= hash >> 33;
        hash *= Prime64_2;
        hash ^= hash >> 29;
        hash *= Prime64_3;
        hash ^= hash >> 32;
        return hash;
    }
}