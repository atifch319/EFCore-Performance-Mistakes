using BenchmarkDotNet.Attributes;
using EFCore.Performance.Api.Data;
using EFCore.Performance.Api.DTOs;
using EFCore.Performance.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace EFCore.Performance.Benchmarks;

[MemoryDiagnoser]
public class ProjectionBenchmarks
{
    private AppDbContext _db = null!;
    private ProjectionQueryService _queries = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        await BenchmarkDatabase.PrepareAsync();
        _db = BenchmarkDatabase.CreateContext();
        _queries = new ProjectionQueryService(_db, NullLogger<ProjectionQueryService>.Instance);
    }

    [GlobalCleanup]
    public async Task GlobalCleanup() => await _db.DisposeAsync();

    [IterationSetup]
    public void ClearTracker() => _db.ChangeTracker.Clear();

    [Benchmark(Baseline = true)]
    public async Task<decimal> FullEntities()
    {
        var rows = await _queries.GetBeforeAsync(0, BenchmarkSettings.QueryTake, CancellationToken.None);
        return Sum(rows);
    }

    [Benchmark]
    public async Task<decimal> ProjectedColumns()
    {
        var rows = await _queries.GetAfterAsync(0, BenchmarkSettings.QueryTake, CancellationToken.None);
        return Sum(rows);
    }

    private static decimal Sum(IReadOnlyList<ProjectedOrderResponse> rows)
    {
        decimal total = 0m;
        foreach (var row in rows)
        {
            total += row.TotalAmount;
        }

        return total;
    }
}
