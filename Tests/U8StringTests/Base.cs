namespace U8.Tests.U8StringTests;

public class Base
{
    public static IEnumerable<object[]> ValidStrings => Constants.ValidStrings.Select(c => new object[] { c });

    [Fact]
    public void Empty_ReturnsCorrectValue()
    {
        var empty = U8String.Empty;

        Assert.True(empty.IsEmpty);
        Assert.Equal(0, empty.Offset);
        Assert.Equal(0, empty.Length);
        Assert.Equal(default, empty);
    }

    [Theory]
    [MemberData(nameof(ValidStrings))]
    public void Length_ReturnsCorrectValue(ReferenceText testCase)
    {
        var u8str = new U8String(testCase.Utf8);
        var expected = testCase.Utf8.Length;

        Assert.Equal(expected, u8str.Length);
    }

    [Theory]
    [MemberData(nameof(ValidStrings))]
    public void IsEmpty_ReturnsCorrectValue(ReferenceText testCase)
    {
        var u8str = new U8String(testCase.Utf8);
        var expected = testCase.Utf8.IsEmpty;

        Assert.Equal(expected, u8str.IsEmpty);
    }

    [Theory]
    [MemberData(nameof(ValidStrings))]
    public void RuneCount_ReturnsCorrectValue(ReferenceText testCase)
    {
        var u8str = new U8String(testCase.Utf8);
        var expected = testCase.Runes.Length;

        Assert.Equal(expected, u8str.RuneCount);
    }

    [Fact]
    public void RuneCount_ReturnsZeroForDefaultValue()
    {
        Assert.Equal(0, default(U8String).RuneCount);
    }

#pragma warning disable CA1829, RCS1077 // Optimize LINQ method call.
    [Theory]
    [MemberData(nameof(ValidStrings))]
    public void Count_ReturnsCorrectValue(ReferenceText testCase)
    {
        var u8str = new U8String(testCase.Utf8);
        var expected = testCase.Utf8.Length;

        Assert.Equal(expected, u8str.Count());
        Assert.Equal(expected, ((ICollection<byte>)u8str).Count);
    }
#pragma warning restore CA1829, RCS1077

    [Fact]
    public void IsReadOnly_ReturnsTrue()
    {
        Assert.True(((ICollection<byte>)default(U8String)).IsReadOnly);
    }

    [Fact]
    public void IsAscii_ReturnsTrueForAsciiStrings()
    {
        Assert.True(default(U8String).IsAscii());
        Assert.True(new U8String(Constants.AsciiBytes).IsAscii());
    }

    [Fact]
    public void IsAscii_ReturnsFalseForNonAsciiStrings()
    {
        Assert.False(new U8String(Constants.CyrilicBytes).IsAscii());
        Assert.False(new U8String(Constants.KanaBytes).IsAscii());
        Assert.False(new U8String(Constants.NonSurrogateEmojiBytes).IsAscii());
        Assert.False(new U8String(Constants.MixedBytes).IsAscii());
    }

    [Theory]
    [MemberData(nameof(ValidStrings))]
    public void IsValid_ReturnsTrueForValidStrings(ReferenceText testCase)
    {
        Assert.True(U8String.IsValid(testCase.Utf8.AsSpan()));
    }

    [Fact]
    public void IsValid_ReturnsTrueForSingleAsciiByte()
    {
        Assert.True(U8String.IsValid([0x00]));
        Assert.True(U8String.IsValid([0x7F]));
    }

    [Fact]
    public void IsValid_ReturnsFalseForInvalidStrings()
    {
        Assert.False(U8String.IsValid([0xC0]));
        Assert.False(U8String.IsValid([0xC0, 0x80]));
        Assert.False(U8String.IsValid([0xC0, 0x80, 0xC0, 0x80]));
        Assert.False(U8String.IsValid([0xC0, 0x80, 0xC0, 0x80, 0xC0, 0x80]));
        Assert.False(U8String.IsValid([.."Hello, World!"u8, 0xC0, 0x80]));
    }

    [Fact]
    public void GetPinnableReference_DereferencingEmptySliceThrowsNRE()
    {
        var values = new[]
        {
            default,
            new U8String(null, int.MaxValue, 0),
            new U8String(null, int.MaxValue / 2, 0)
        };

        foreach (var value in values)
        {
            Assert.Throws<NullReferenceException>(() =>
            {
                ref readonly var ptr = ref value.GetPinnableReference();
                _ = ref ptr;
            });
        }
    }

    [Fact]
    public void Insert_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() => ((IList<byte>)default(U8String)).Insert(0, 0));
    }

    [Fact]
    public void RemoveAt_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() => ((IList<byte>)default(U8String)).RemoveAt(0));
    }

    [Fact]
    public void Add_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() => ((ICollection<byte>)default(U8String)).Add(0));
    }

    [Fact]
    public void Clear_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() => ((ICollection<byte>)default(U8String)).Clear());
    }

    [Fact]
    public void Remove_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() => ((ICollection<byte>)default(U8String)).Remove(0));
    }
}
