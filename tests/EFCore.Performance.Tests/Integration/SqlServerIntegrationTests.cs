using System.Data;
using EFCore.Performance.Api.Data;
using EFCore.Performance.Api.DTOs;
using EFCore.Performance.Api.Services;
using EFCore.Performance.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.Performance.Tests.Integration;

[Collection(SqlServerApiCollection.Name)]
[Trait("Category", "SqlServer")]
public sealed class SqlServerIntegrationTests(SqlServerApiFactory factory)
{
    [SqlServerFact]
    public async Task Tracking_BeforeAndAfter_ReturnTheSamePage()
    {
        var client = factory.CreateClient();
        var before = await ApiClient.GetRequiredAsync<List<OrderReadResponse>>(client, "/api/tracking/before?take=10");
        var after = await ApiClient.GetRequiredAsync<List<OrderReadResponse>>(client, "/api/tracking/after?take=10");

        Assert.Equal(before, after);
        Assert.Equal("ORD-0001", after[0].OrderNumber);
        Assert.Equal(170.00m, after[0].TotalAmount);
    }

    [SqlServerFact]
    public async Task NPlusOne_ReturnsTheSameRows_AndExecutesFewerSqlCommands()
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
        Assert.True(afterCommands.Count < beforeCommands.Count, Explain(beforeCommands, afterCommands));
        Assert.All(after, row => Assert.Equal(DemoSeed.ItemsPerOrder, row.ItemCount));
    }

    [SqlServerFact]
    public async Task Materialization_BeforeAndAfter_ReturnTheSameFilteredPage()
    {
        var client = factory.CreateClient();
        const string query = "?take=20&status=Shipped&placedAfter=2024-02-01T00:00:00Z";
        var before = await ApiClient.GetRequiredAsync<List<FilteredOrderResponse>>(client, "/api/materialization/before" + query);
        var after = await ApiClient.GetRequiredAsync<List<FilteredOrderResponse>>(client, "/api/materialization/after" + query);

        Assert.Equal(before, after);
        Assert.NotEmpty(after);
        Assert.All(after, row => Assert.Equal("Shipped", row.Status));
    }

    [SqlServerFact]
    public async Task SqlInspection_BeforeAndAfter_ReturnTheSameFilteredRows()
    {
        var client = factory.CreateClient();
        const string query = "?take=10&status=Paid&placedAfter=2024-02-01T00:00:00Z";
        var before = await ApiClient.GetRequiredAsync<List<SqlInspectionOrderResponse>>(client, "/api/sql-inspection/before" + query);
        var after = await ApiClient.GetRequiredAsync<List<SqlInspectionOrderResponse>>(client, "/api/sql-inspection/after" + query);

        Assert.Equal(before, after);
        Assert.NotEmpty(after);
        Assert.All(after, row =>
        {
            Assert.Equal("Paid", row.Status);
            Assert.True(row.PlacedAt >= OrderFilterParser.DefaultPlacedAfter);
        });
    }

    [SqlServerFact]
    public async Task Projection_BeforeAndAfter_ReturnTheSameFields()
    {
        var client = factory.CreateClient();
        var before = await ApiClient.GetRequiredAsync<List<ProjectedOrderResponse>>(client, "/api/projection/before?take=15");
        var after = await ApiClient.GetRequiredAsync<List<ProjectedOrderResponse>>(client, "/api/projection/after?take=15");

        Assert.Equal(before, after);
        Assert.Equal(15, after.Count);
    }

    [SqlServerFact]
    public async Task Diagnostics_OnSqlServer_ProjectOnlyTheRequiredColumns()
    {
        var client = factory.CreateClient();
        var diagnostics = await ApiClient.GetRequiredAsync<SqlDiagnosticsResponse>(
            client,
            "/api/sql-inspection/diagnostics?take=5&status=Shipped&placedAfter=2024-02-01T00:00:00Z");

        Assert.Contains("[InternalNotes]", diagnostics.ProjectionBeforeSql, StringComparison.Ordinal);
        Assert.Contains("[Email]", diagnostics.ProjectionBeforeSql, StringComparison.Ordinal);
        Assert.Contains("[OrderNumber]", diagnostics.ProjectionAfterSql, StringComparison.Ordinal);
        Assert.Contains("[TotalAmount]", diagnostics.ProjectionAfterSql, StringComparison.Ordinal);
        Assert.Contains("[Name]", diagnostics.ProjectionAfterSql, StringComparison.Ordinal);
        Assert.DoesNotContain("[InternalNotes]", diagnostics.ProjectionAfterSql, StringComparison.Ordinal);
        Assert.DoesNotContain("[ShippingAddress]", diagnostics.ProjectionAfterSql, StringComparison.Ordinal);
        Assert.DoesNotContain("[Email]", diagnostics.ProjectionAfterSql, StringComparison.Ordinal);
        Assert.DoesNotContain("[Description]", diagnostics.ProjectionAfterSql, StringComparison.Ordinal);
        Assert.DoesNotContain("[Notes]", diagnostics.ProjectionAfterSql, StringComparison.Ordinal);

        Assert.DoesNotContain("WHERE", diagnostics.MaterializationBeforeSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WHERE", diagnostics.MaterializationAfterSql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[Status]", diagnostics.MaterializationAfterSql, StringComparison.Ordinal);
        Assert.Contains("FETCH", diagnostics.ProjectionAfterSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("[InternalNotes]", diagnostics.SqlInspectionAfterSql, StringComparison.Ordinal);
        Assert.Contains("WHERE", diagnostics.SqlInspectionAfterSql, StringComparison.OrdinalIgnoreCase);
    }

    [SqlServerFact]
    public async Task Seeding_Twice_DoesNotDuplicateTheDemoDataset()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var first = await DatabaseSeeder.SeedAsync(db);
        var second = await DatabaseSeeder.SeedAsync(db);

        Assert.False(first.Inserted);
        Assert.False(second.Inserted);
        Assert.Equal(DemoSeed.CustomerCount, second.Customers);
        Assert.Equal(DemoSeed.OrderCount, second.Orders);
        Assert.Equal(DemoSeed.OrderItemCount, second.OrderItems);
        Assert.Equal(DemoSeed.ProductCount, second.Products);
        Assert.Equal(first, second);
    }

    [SqlServerFact]
    public async Task Migration_CreatesTheFilteredReadIndex()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync();
        }

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sys.indexes WHERE name = @name";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@name";
        parameter.Value = "IX_Orders_Status_PlacedAt_Id";
        command.Parameters.Add(parameter);

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        Assert.Equal(1, count);
    }

    [SqlServerFact]
    public async Task MissingOrder_ReturnsNotFound()
    {
        var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/tracking/orders/2147483647");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Order not found", body, StringComparison.Ordinal);
        Assert.DoesNotContain("StackTrace", body, StringComparison.OrdinalIgnoreCase);
    }

    private static string Explain(IReadOnlyList<string> before, IReadOnlyList<string> after) =>
        $"Before ({before.Count}): {string.Join(" || ", before)}{Environment.NewLine}After ({after.Count}): {string.Join(" || ", after)}";
}
