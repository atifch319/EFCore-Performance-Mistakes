using EFCore.Performance.Api.Data;
using EFCore.Performance.Api.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Performance.Tests.Unit;

// Uses SQLite only to exercise idempotent seeding. This is not a SQL Server verification.
public sealed class DatabaseSeederTests
{
    [Fact]
    public async Task Seed_CanBeInvokedTwice_WithoutDuplicatingRows()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var first = await DatabaseSeeder.SeedAsync(db);
        var second = await DatabaseSeeder.SeedAsync(db);

        Assert.True(first.Inserted);
        Assert.False(second.Inserted);
        Assert.Equal(DemoSeed.CustomerCount, second.Customers);
        Assert.Equal(DemoSeed.ProductCount, second.Products);
        Assert.Equal(DemoSeed.OrderCount, second.Orders);
        Assert.Equal(DemoSeed.OrderItemCount, second.OrderItems);
        Assert.Equal(first.Customers, second.Customers);
        Assert.Equal(first.Orders, second.Orders);
        Assert.Equal(first.OrderItems, second.OrderItems);
        Assert.Equal(first.Products, second.Products);

        var order = await db.Orders.SingleAsync(candidate => candidate.OrderNumber == "ORD-0001");
        Assert.Equal(170.00m, order.TotalAmount);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(DemoSeed.OrderPlacedAt(1), order.PlacedAt);
        Assert.Equal(DemoSeed.ItemsPerOrder, await db.OrderItems.CountAsync(item => item.OrderId == order.Id));
    }
}
