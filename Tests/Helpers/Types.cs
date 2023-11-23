using System.Collections.Immutable;
using System.Text;

namespace U8Primitives.Tests;

public record ReferenceText(
    string Name,
    string Utf16,
    ImmutableArray<byte> Utf8,
    ImmutableArray<Rune> Runes);