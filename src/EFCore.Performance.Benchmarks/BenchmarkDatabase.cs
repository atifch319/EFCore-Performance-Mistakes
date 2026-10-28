using EFCore.Performance.Api.Data;
using EFCore.Performance.Api.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Performance.Benchmarks;

internal static class BenchmarkDatabase
{
    public static string ConnectionString { get; } = BuildConnectionString();

    public static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    public static async Task PrepareAsync(CancellationToken cancellationToken = default)
    {
        var catalog = new SqlConnectionStringBuilder(ConnectionString).InitialCatalog;
        if (!string.Equals(catalog, BenchmarkSettings.DatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Refusing to prepare '{catalog}'. Benchmarks only create {BenchmarkSettings.DatabaseName}.");
        }

        await using var db = CreateContext();
        await db.Database.MigrateAsync(cancellationToken);
        if (await IsCurrentSeedAsync(db, cancellationToken))
        {
            return;
        }

        await db.Database.EnsureDeletedAsync(cancellationToken);

        await using var recreated = CreateContext();
        await recreated.Database.MigrateAsync(cancellationToken);
        recreated.AddRange(BuildGraph());
        await recreated.SaveChangesAsync(cancellationToken);
    }

    private static async Task<bool> IsCurrentSeedAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var customers = await db.Customers.CountAsync(cancellationToken);
        var orders = await db.Orders.CountAsync(cancellationToken);
        return customers == BenchmarkSettings.CustomerCount && orders == BenchmarkSettings.OrderCount;
    }

    private static string BuildConnectionString()
    {
        var configured = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        var source = string.IsNullOrWhiteSpace(configured)
            ? "Server=localhost;Database=EFCorePerformanceDemo;Trusted_Connection=True;TrustServerCertificate=True"
            : configured;

        var builder = new SqlConnectionStringBuilder(source)
        {
            InitialCatalog = BenchmarkSettings.DatabaseName
        };

        if (!string.Equals(builder.InitialCatalog, BenchmarkSettings.DatabaseName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Benchmark catalog was rewritten to '{builder.InitialCatalog}'.");
        }

        return builder.ConnectionString;
    }

    private static List<object> BuildGraph()
    {
        var products = new List<Product>(BenchmarkSettings.ProductCount);
        for (var number = 1; number <= BenchmarkSettings.ProductCount; number++)
        {
            products.Add(new Product
            {
                Sku = $"BENCH-{number:000}",
                Name = $"Benchmark Product {number:00}",
                Description = $"Benchmark catalog text {number:00}. " + new string('d', 400),
                ListPrice = 10m + number,
                IsActive = true
            });
        }

        var customers = new List<Customer>(BenchmarkSettings.CustomerCount);
        for (var customerNumber = 1; customerNumber <= BenchmarkSettings.CustomerCount; customerNumber++)
        {
            var customer = new Customer
            {
                Name = $"Benchmark Customer {customerNumber:000}",
                Email = $"bench{customerNumber:000}@example.test",
                Notes = $"Benchmark customer notes {customerNumber:000}. " + new string('n', 400),
                CreatedAt = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(customerNumber)
            };

            for (var ordinal = 1; ordinal <= BenchmarkSettings.OrdersPerCustomer; ordinal++)
            {
                var sequence = ((customerNumber - 1) * BenchmarkSettings.OrdersPerCustomer) + ordinal;
                var order = new Order
                {
                    OrderNumber = $"B-{sequence:000000}",
                    Status = (OrderStatus)((sequence - 1) % 4),
                    PlacedAt = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero).AddHours(sequence),
                    ShippingAddress = $"{sequence} Benchmark Street, Sample City",
                    InternalNotes = $"Benchmark internal notes {sequence:000000}. " + new string('i', 400)
                };

                decimal total = 0m;
                for (var itemNumber = 1; itemNumber <= BenchmarkSettings.ItemsPerOrder; itemNumber++)
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
}
