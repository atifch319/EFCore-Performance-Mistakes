using EFCore.Performance.Api.Entities;
using EFCore.Performance.Api.Services;

namespace EFCore.Performance.Tests.Unit;

public sealed class OrderFilterParserTests
{
    [Fact]
    public void Defaults_ToShippedOrders_OnOrAfterFebruary2024()
    {
        var parsed = OrderFilterParser.TryParse(null, null, out var filter, out var error);

        Assert.True(parsed);
        Assert.Null(error);
        Assert.Equal(OrderStatus.Shipped, filter.Status);
        Assert.Equal(OrderFilterParser.DefaultPlacedAfter, filter.PlacedAfter);
    }

    [Theory]
    [InlineData("shipped", OrderStatus.Shipped)]
    [InlineData("Paid", OrderStatus.Paid)]
    [InlineData("CANCELLED", OrderStatus.Cancelled)]
    public void Parses_StatusNames_WithoutCaseSensitivity(string status, OrderStatus expected)
    {
        var parsed = OrderFilterParser.TryParse(status, null, out var filter, out _);

        Assert.True(parsed);
        Assert.Equal(expected, filter.Status);
    }

    [Theory]
    [InlineData("2")]
    [InlineData("NotAStatus")]
    public void Rejects_NumericAndUnknownStatusValues(string status)
    {
        var parsed = OrderFilterParser.TryParse(status, null, out _, out var error);

        Assert.False(parsed);
        Assert.Contains("Pending", error, StringComparison.Ordinal);
    }
}
