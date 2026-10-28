using EFCore.Performance.Api.Services;

namespace EFCore.Performance.Tests.Unit;

public sealed class ListWindowParserTests
{
    [Fact]
    public void Defaults_SkipToZero_AndTakeToTwenty()
    {
        var parsed = ListWindowParser.TryParse(null, null, out var window, out var error);

        Assert.True(parsed);
        Assert.Null(error);
        Assert.Equal(0, window.Skip);
        Assert.Equal(QueryLimits.DefaultTake, window.Take);
    }

    [Theory]
    [InlineData(-1, 10)]
    [InlineData(1_001, 10)]
    [InlineData(0, 0)]
    [InlineData(0, 51)]
    public void Rejects_ValuesOutsideTheDocumentedBounds(int skip, int take)
    {
        var parsed = ListWindowParser.TryParse(skip, take, out _, out var error);

        Assert.False(parsed);
        Assert.False(string.IsNullOrWhiteSpace(error));
    }
}
