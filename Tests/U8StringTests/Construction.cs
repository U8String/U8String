namespace U8Primitives.Tests.U8StringTests;

public class Construction
{
    [Fact]
    public void CtorArrayIntInt_DropsReferenceWhenLengthIsZero()
    {
        var bytes = new byte[7];
        var offsets = new[] { 0, 1, 3, 7 };

        foreach (var offset in offsets)
        {
            var str = new U8String(bytes, offset, 0);

            Assert.Null(str._value);
            Assert.True(str.IsEmpty);
            Assert.Equal(offset, str.Offset);
            Assert.Equal(0, str.Length);
        }
    }

    [Fact]
    public void CtorArrayRange_DropsReferenceWhenLengthIsZero()
    {
        var bytes = new byte[7];
        var offsets = new[] { 0, 1, 3, 7 };

        foreach (var offset in offsets)
        {
            var range = new U8Range(offset, 0);
            var str = new U8String(bytes, range);

            Assert.Null(str._value);
            Assert.True(str.IsEmpty);
            Assert.Equal(offset, str.Offset);
            Assert.Equal(0, str.Length);
        }
    }
}
