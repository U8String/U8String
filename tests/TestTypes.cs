using System.Text;

namespace U8Primitives.Tests;

public record TestCase(
    string Name,
    string Utf16,
    byte[] Utf8,
    Rune[] Runes);