using System.Collections.Immutable;
using System.Text;

namespace U8.Tests;

public record ReferenceText(
    string Name,
    string Utf16,
    ImmutableArray<byte> Utf8,
    ImmutableArray<Rune> Runes);