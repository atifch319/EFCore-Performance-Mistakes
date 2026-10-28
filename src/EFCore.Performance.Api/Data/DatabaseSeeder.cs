using EFCore.Performance.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Performance.Api.Data;

public static class DatabaseSeeder
{
    public static async Task<SeedResult> SeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        var counts = await ReadCountsAsync(db, cancellationToken);
        if (IsEmpty(counts))
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            db.AddRange(BuildGraph());
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var inserted = await ReadCountsAsync(db, cancellationToken);
            return new SeedResult(true, inserted.Customers, inserted.Orders, inserted.OrderItems, inserted.Products);
        }

        if (IsDemoDataset(counts))
        {
            return new SeedResult(false, counts.Customers, counts.Orders, counts.OrderItems, counts.Products);
        }

        throw new InvalidOperationException(
            "Refusing to seed because the database already contains rows that do not match the deterministic demo dataset. Use an empty demo database.");
    }

    private static bool IsEmpty(TableCounts counts) =>
        counts.Customers == 0 && counts.Orders == 0 && counts.OrderItems == 0 && counts.Products == 0;

    private static bool IsDemoDataset(TableCounts counts) =>
        counts.Customers == DemoSeed.CustomerCount
        && counts.Orders == DemoSeed.OrderCount
        && counts.OrderItems == DemoSeed.OrderItemCount
        && counts.Products == DemoSeed.ProductCount;

    private static async Task<TableCounts> ReadCountsAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        return new TableCounts(
            await db.Customers.AsNoTracking().CountAsync(cancellationToken),
            await db.Orders.AsNoTracking().CountAsync(cancellationToken),
            await db.OrderItems.AsNoTracking().CountAsync(cancellationToken),
            await db.Products.AsNoTracking().CountAsync(cancellationToken));
    }

    private static List<object> BuildGraph()
    {
        var products = new List<Product>(DemoSeed.ProductCount);
        for (var number = 1; number <= DemoSeed.ProductCount; number++)
        {
            products.Add(new Product
            {
                Sku = $"SKU-{number:000}",
                Name = $"Product {number:00}",
                Description = DemoText.ProductDescription(number),
                ListPrice = 10m + number,
                IsActive = number % 7 != 0
            });
        }

        var customers = new List<Customer>(DemoSeed.CustomerCount);
        for (var customerNumber = 1; customerNumber <= DemoSeed.CustomerCount; customerNumber++)
        {
            var customer = new Customer
            {
                Name = $"Customer {customerNumber:00}",
                Email = $"customer{customerNumber:00}@example.test",
                Notes = DemoText.CustomerNotes(customerNumber),
                CreatedAt = DemoSeed.CustomerCreatedAt(customerNumber)
            };

            for (var ordinal = 1; ordinal <= DemoSeed.OrdersPerCustomer; ordinal++)
            {
                var sequence = ((customerNumber - 1) * DemoSeed.OrdersPerCustomer) + ordinal;
                var order = new Order
                {
                    OrderNumber = $"ORD-{sequence:0000}",
                    Status = (OrderStatus)((sequence - 1) % 4),
                    PlacedAt = DemoSeed.OrderPlacedAt(sequence),
                    ShippingAddress = $"{100 + sequence} Demo Street, Sample City",
                    InternalNotes = DemoText.InternalNotes(sequence)
                };

                decimal total = 0m;
                for (var itemNumber = 1; itemNumber <= DemoSeed.ItemsPerOrder; itemNumber++)
                {
                    var product = products[(sequence + itemNumber) % products.Count];
                    var quantity = (sequence + itemNumber) % 5 + 1;
                    total += quantity * product.ListPrice;
                    order.Items.Add(new OrderItem
                    {
                        Product = product,
                        Quantity = quantity,
                        UnitPrice = product.ListPrice
                    });
                }

                order.TotalAmount = total;
                customer.Orders.Add(order);
            }

            customers.Add(customer);
        }

        var graph = new List<object>(products.Count + customers.Count);
        graph.AddRange(products);
        graph.AddRange(customers);
        return graph;
    }

    private readonly record struct TableCounts(int Customers, int Orders, int OrderItems, int Products);
}
