using EFCore.Performance.Api.Data;
using EFCore.Performance.Api.DTOs;
using EFCore.Performance.Api.Services;
using EFCore.Performance.Tests.Support;

namespace EFCore.Performance.Tests.Integration;

[Collection(SqliteApiCollection.Name)]
public sealed class SqliteEndpointTests(SqliteApiFactory factory)
{
    [Fact]
    public async Task Tracking_BeforeAndAfter_ReturnTheSamePage()
    {
        var client = factory.CreateClient();

        var before = await ApiClient.GetRequiredAsync<List<OrderReadResponse>>(client, "/api/tracking/before?take=10");
        var after = await ApiClient.GetRequiredAsync<List<OrderReadResponse>>(client, "/api/tracking/after?take=10");
        var skipped = await ApiClient.GetRequiredAsync<List<OrderReadResponse>>(client, "/api/tracking/after?skip=5&take=5");

        Assert.Equal(before, after);
        Assert.Equal(10, after.Count);
        Assert.Equal("ORD-0001", after[0].OrderNumber);
        Assert.Equal("Customer 01", after[0].CustomerName);
        Assert.Equal(170.00m, after[0].TotalAmount);
        Assert.Equal("Pending", after[0].Status);
        Assert.Equal(DemoSeed.OrderPlacedAt(1), after[0].PlacedAt);
        Assert.Equal(after[5].Id, skipped[0].Id);
        Assert.Equal(5, skipped.Count);
    }

    [Fact]
    public async Task NPlusOne_BeforeAndAfter_ReturnTheSameSummaries()
    {
        var client = factory.CreateClient();

        var before = await ApiClient.GetRequiredAsync<List<NPlusOneOrderResponse>>(client, "/api/nplusone/before?take=10");
        var after = await ApiClient.GetRequiredAsync<List<NPlusOneOrderResponse>>(client, "/api/nplusone/after?take=10");

        Assert.Equal(before, after);
        Assert.All(after, row => Assert.Equal(DemoSeed.ItemsPerOrder, row.ItemCount));
        Assert.Equal("Customer 01", after[0].CustomerName);
        Assert.Contains(after, row => row.CustomerName == "Customer 02");
    }

    [Fact]
    public async Task NPlusOne_OnSqlite_UsesFewerCommandsThanThePerOrderLoop()
    {
        var client = factory.CreateClient();
        factory.Interceptor.Reset();

        var before = await ApiClient.GetRequiredAsync<List<NPlusOneOrderResponse>>(client, "/api/nplusone/before?take=8");
        var beforeCommands = factory.Interceptor.Snapshot();

        factory.Interceptor.Reset();
        var after = await ApiClient.GetRequiredAsync<List<NPlusOneOrderResponse>>(client, "/api/nplusone/after?take=8");
        var afterCommands = factory.Interceptor.Snapshot();

        Assert.Equal(before, after);
        Assert.Equal(1 + (2 * before.Count), beforeCommands.Count);
        Assert.True(
            afterCommands.Count < beforeCommands.Count,
            FormatCommands(beforeCommands, afterCommands));
    }

    [Fact]
    public async Task Materialization_Before_FiltersTheBoundedPageInMemory()
    {
        // SQLite does not translate DateTimeOffset comparisons, so the composed SQL Server query
        // is asserted in SqlServerIntegrationTests. This check covers the in-memory path only.
        var client = factory.CreateClient();
        var before = await ApiClient.GetRequiredAsync<List<FilteredOrderResponse>>(
            client,
            "/api/materialization/before?take=20&status=Shipped&placedAfter=2024-02-01T00:00:00Z");

        Assert.NotEmpty(before);
        Assert.True(before.Count < DemoSeed.OrderCount);
        Assert.All(before, row =>
        {
            Assert.Equal("Shipped", row.Status);
            Assert.True(row.PlacedAt >= OrderFilterParser.DefaultPlacedAfter);
        });
    }

    [Fact]
    public async Task Projection_BeforeAndAfter_ReturnTheSameFields()
    {
        var client = factory.CreateClient();

        var before = await ApiClient.GetRequiredAsync<List<ProjectedOrderResponse>>(client, "/api/projection/before?take=15");
        var after = await ApiClient.GetRequiredAsync<List<ProjectedOrderResponse>>(client, "/api/projection/after?take=15");

        Assert.Equal(before, after);
        Assert.Equal(15, after.Count);
        Assert.DoesNotContain("Email", after[0].ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Customer 01", after[0].CustomerName);
    }

    [Fact]
    public async Task MissingOrder_ReturnsNotFound_WithoutExceptionDetails()
    {
        var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/tracking/orders/2147483647");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Order not found", body, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Exception", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvalidTake_ReturnsBadRequest()
    {
        var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/tracking/before?take=0");

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task InvalidStatus_ReturnsBadRequest()
    {
        var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/materialization/after?status=NotAStatus");

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static string FormatCommands(IReadOnlyList<string> before, IReadOnlyList<string> after) =>
        $"SQLite command counts are a relational round-trip check, not a SQL Server performance result.{Environment.NewLine}Before ({before.Count}): {string.Join(" || ", before)}{Environment.NewLine}After ({after.Count}): {string.Join(" || ", after)}";
}
