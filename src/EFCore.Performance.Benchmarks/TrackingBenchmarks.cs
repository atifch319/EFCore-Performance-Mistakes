using BenchmarkDotNet.Attributes;
using EFCore.Performance.Api.Data;
using EFCore.Performance.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Performance.Benchmarks;

[MemoryDiagnoser]
public class TrackingBenchmarks
{
    private AppDbContext _db = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        await BenchmarkDatabase.PrepareAsync();
        _db = BenchmarkDatabase.CreateContext();
    }

    [GlobalCleanup]
    public async Task GlobalCleanup() => await _db.DisposeAsync();

    [IterationSetup]
    public void ClearTracker() => _db.ChangeTracker.Clear();

    [Benchmark(Baseline = true)]
    public Task<decimal> Tracked() =>
        ConsumeAsync(_db.Orders.Include(order => order.Customer).OrderBy(order => order.Id).Take(BenchmarkSettings.QueryTake));

    [Benchmark]
    public Task<decimal> NoTracking() =>
        ConsumeAsync(_db.Orders.AsNoTracking().Include(order => order.Customer).OrderBy(order => order.Id).Take(BenchmarkSettings.QueryTake));

    [Benchmark]
    public Task<decimal> NoTrackingWithIdentityResolution() =>
        ConsumeAsync(_db.Orders.AsNoTrackingWithIdentityResolution().Include(order => order.Customer).OrderBy(order => order.Id).Take(BenchmarkSettings.QueryTake));

    private static async Task<decimal> ConsumeAsync(IQueryable<Order> query)
    {
        var orders = await query.ToListAsync();
        decimal total = 0m;
        foreach (var order in orders)
        {
            total += order.TotalAmount;
            total += order.Customer.Name.Length;
        }

        return total;
    }
}
