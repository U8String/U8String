using System.Text;

using U8.Primitives;

namespace U8.Tests;

// These tests perform exhaustive evaluation and cannot be theories.
public class U8ScalarTests
{
    public static IEnumerable<object[]> Runes => Constants.AllRunes.Select(r => new object[] { r });

    [Fact]
    public void U8Scalar_CreateFromByteReturnsCorrectValue()
    {
        foreach (var b in Constants.AsciiBytes)
        {
            var scalar = new U8Scalar(b);
            var message = $"Byte: {b} (0x{b:X})";

            Assert.True(new[] { b }.AsSpan().SequenceEqual(scalar.AsSpan()), message);
            Assert.Equal(1, scalar.Length);
        }
    }

    [Fact]
    public void U8Scalar_CreateFromCharReturnsCorrectValue()
    {
        foreach (var c in Enumerable
            .Range(char.MinValue, char.MaxValue)
            .Select(i => (char)i)
            .Where(c => !char.IsSurrogate(c)))
        {
            var bytes = Encoding.UTF8.GetBytes(c.ToString());
            var scalar = new U8Scalar(c);
            var message = $"Char: {c} (0x{(int)c:X})";

            Assert.True(bytes.AsSpan().SequenceEqual(scalar.AsSpan()), message);
            Assert.Equal(bytes.Length, scalar.Length);
        }
    }

    [Fact]
    public void U8Scalar_CreateFromRuneReturnsCorrectValue()
    {
        foreach (var rune in Constants.AllRunes)
        {
            var bytes = rune.ToUtf8();
            var scalar = new U8Scalar(rune);
            var message = $"Rune: {rune} (0x{rune.Value:X})";

            Assert.True(bytes.AsSpan().SequenceEqual(scalar.AsSpan()), message);
            Assert.Equal(bytes.Length, scalar.Length);
        }
    }
}
