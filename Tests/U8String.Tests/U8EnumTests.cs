using System.Net;

namespace U8.Tests;

public partial class U8EnumTests
{
    public static TheoryData<object[]> Enums = new(
    [
        Enum.GetValues<UriKind>(),
        Enum.GetValues<TypeCode>(),
        Enum.GetValues<DayOfWeek>(),
        Enum.GetValues<ConsoleColor>(),
        Enum.GetValues<HttpStatusCode>(),

        Enum.GetValues<ByteEnum>(),
        Enum.GetValues<SByteEnum>(),
        Enum.GetValues<ShortEnum>(),
        Enum.GetValues<UShortEnum>(),
        Enum.GetValues<IntEnum>(),
        Enum.GetValues<UIntEnum>(),
        Enum.GetValues<LongEnum>(),
        Enum.GetValues<ULongEnum>()
    ]);

    [Fact]
    public void GetName_ProducesCorrectValueForContiguous()
    {
        foreach (var value in Enum.GetValues<ConsoleColor>())
        {
            var expected = u8(Enum.GetName(value)!);
            var actual = U8Enum.GetName(value);

            Assert.NotNull(actual);
            var notnull = actual.Value;
            Assert.Equal(expected, notnull);
            Assert.True(notnull.IsNullTerminated);
            // Test that the name is cached
            Assert.Equal(notnull.Source, U8Enum.GetName(value)!.Value.Source);
            // Verify that the name is allocated on the pinned object heap
            Assert.Equal(2, GC.GetGeneration(notnull._value!));
        }
    }

    [Fact]
    public void GetName_ProducesCorrectValueForNonContiguous()
    {
        foreach (var value in Enum.GetValues<NonContiguousEnum>())
        {
            var expected = u8(Enum.GetName(value)!);
            var actual = U8Enum.GetName(value);

            Assert.NotNull(actual);
            var notnull = actual.Value;
            Assert.Equal(expected, notnull);
            Assert.True(notnull.IsNullTerminated);
            // Test that the name is cached
            Assert.Equal(notnull.Source, U8Enum.GetName(value)!.Value.Source);
            // Verify that the name is allocated on the pinned object heap
            Assert.Equal(2, GC.GetGeneration(notnull._value!));
        }
    }

    [Fact(Skip = "This is pending a fix for in U8EnumFormattable<T>.ToU8String()")]
    public void GetName_IsBugCompatibleWithCoreLibForEnumsWithDuplicateValues()
    {
        foreach (var value in Enum.GetValues<HttpStatusCode>())
        {
            var expected = u8(Enum.GetName(value)!);
            var actual = U8Enum.GetName(value);

            Assert.NotNull(actual);
            var notnull = actual.Value;
            Assert.Equal(expected, notnull);
            Assert.True(notnull.IsNullTerminated);
            // Test that the name is cached
            Assert.Equal(notnull.Source, U8Enum.GetName(value)!.Value.Source);
            // Verify that the name is allocated on the pinned object heap
            Assert.Equal(2, GC.GetGeneration(notnull._value!));
        }
    }

    [Fact]
    public void GetNames_ProducesCorrectValue()
    {
        Assert.Equal(
            Enum.GetNames<UriKind>().Select(u8),
            U8Enum.GetNames<UriKind>());

        Assert.Equal(
            Enum.GetNames<TypeCode>().Select(u8),
            U8Enum.GetNames<TypeCode>());

        Assert.Equal(
            Enum.GetNames<DayOfWeek>().Select(u8),
            U8Enum.GetNames<DayOfWeek>());

        Assert.Equal(
            Enum.GetNames<ConsoleColor>().Select(u8),
            U8Enum.GetNames<ConsoleColor>());

        Assert.Equal(
            Enum.GetNames<HttpStatusCode>().Select(u8),
            U8Enum.GetNames<HttpStatusCode>());

        Assert.Equal(
            Enum.GetNames<ByteEnum>().Select(u8),
            U8Enum.GetNames<ByteEnum>());

        Assert.Equal(
            Enum.GetNames<SByteEnum>().Select(u8),
            U8Enum.GetNames<SByteEnum>());

        Assert.Equal(
            Enum.GetNames<ShortEnum>().Select(u8),
            U8Enum.GetNames<ShortEnum>());

        Assert.Equal(
            Enum.GetNames<UShortEnum>().Select(u8),
            U8Enum.GetNames<UShortEnum>());

        Assert.Equal(
            Enum.GetNames<IntEnum>().Select(u8),
            U8Enum.GetNames<IntEnum>());
        
        Assert.Equal(
            Enum.GetNames<UIntEnum>().Select(u8),
            U8Enum.GetNames<UIntEnum>());

        Assert.Equal(
            Enum.GetNames<LongEnum>().Select(u8),
            U8Enum.GetNames<LongEnum>());

        Assert.Equal(
            Enum.GetNames<ULongEnum>().Select(u8),
            U8Enum.GetNames<ULongEnum>());

        Assert.Equal(
            Enum.GetNames<NonContiguousEnum>().Select(u8),
            U8Enum.GetNames<NonContiguousEnum>());
    }

    [Fact]
    public void GetValues_ProducesCorrectValue()
    {
        Assert.Equal(
            Enum.GetValues<UriKind>(),
            U8Enum.GetValues<UriKind>());

        Assert.Equal(
            Enum.GetValues<TypeCode>(),
            U8Enum.GetValues<TypeCode>());

        Assert.Equal(
            Enum.GetValues<DayOfWeek>(),
            U8Enum.GetValues<DayOfWeek>());

        Assert.Equal(
            Enum.GetValues<ConsoleColor>(),
            U8Enum.GetValues<ConsoleColor>());

        Assert.Equal(
            Enum.GetValues<HttpStatusCode>(),
            U8Enum.GetValues<HttpStatusCode>());

        Assert.Equal(
            Enum.GetValues<ByteEnum>(),
            U8Enum.GetValues<ByteEnum>());

        Assert.Equal(
            Enum.GetValues<SByteEnum>(),
            U8Enum.GetValues<SByteEnum>());

        Assert.Equal(
            Enum.GetValues<ShortEnum>(),
            U8Enum.GetValues<ShortEnum>());

        Assert.Equal(
            Enum.GetValues<UShortEnum>(),
            U8Enum.GetValues<UShortEnum>());

        Assert.Equal(
            Enum.GetValues<IntEnum>(),
            U8Enum.GetValues<IntEnum>());

        Assert.Equal(
            Enum.GetValues<UIntEnum>(),
            U8Enum.GetValues<UIntEnum>());

        Assert.Equal(
            Enum.GetValues<LongEnum>(),
            U8Enum.GetValues<LongEnum>());

        Assert.Equal(
            Enum.GetValues<ULongEnum>(),
            U8Enum.GetValues<ULongEnum>());

        Assert.Equal(
            Enum.GetValues<NonContiguousEnum>(),
            U8Enum.GetValues<NonContiguousEnum>());
    }

    [Fact]
    public void Parse_ParsesNamesCorrectly()
    {
        static void AssertAll<T>() where T : struct, Enum
        {
            foreach (var name in Enum.GetNames<T>())
            {
                var expected = Enum.Parse<T>(name);
                var actual = U8Enum.Parse<T>(u8(name));
                Assert.Equal(expected, actual);
            }
        }

        AssertAll<UriKind>();
        AssertAll<TypeCode>();
        AssertAll<DayOfWeek>();
        AssertAll<ConsoleColor>();
        AssertAll<HttpStatusCode>();

        AssertAll<ByteEnum>();
        AssertAll<SByteEnum>();
        AssertAll<ShortEnum>();
        AssertAll<UShortEnum>();
        AssertAll<IntEnum>();
        AssertAll<UIntEnum>();
        AssertAll<LongEnum>();
        AssertAll<ULongEnum>();
        AssertAll<NonContiguousEnum>();
    }

    [Fact]
    public void Parse_CanParseEnumsWithDuplicateValues()
    {
        var ambiguous = u8(nameof(HttpStatusCode.Ambiguous));
        var multipleChoices = u8(nameof(HttpStatusCode.MultipleChoices));

        Assert.Equal(
            U8Enum.Parse<HttpStatusCode>(ambiguous),
            U8Enum.Parse<HttpStatusCode>(multipleChoices));
    }
}