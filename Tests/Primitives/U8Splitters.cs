namespace U8.Tests.Primitives;

public class U8SplitterTests
{
    // TODO: Add non-smoke tests
    [Fact]
    public void FindSegment_ReturnsCorrectValue()
    {
        var values = (U8String)"random|applebee|apple|testapp|app|test"u8;
        var split = values.Split('|');

        Assert.Equal(16, split.FindOffset("apple"u8));
        Assert.Equal(7, split.FindOffset("applebee"u8));
        Assert.Equal(22, split.FindOffset("testapp"u8));
        Assert.Equal(30, split.FindOffset("app"u8));
        Assert.Equal(34, split.FindOffset("test"u8));
        Assert.Equal(-1, split.FindOffset("tes"u8));
        Assert.Equal(-1, split.FindOffset("bee"u8));
    }
}