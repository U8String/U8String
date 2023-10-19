namespace U8Primitives.Tests.U8StringTests;

public class Base
{
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
}
