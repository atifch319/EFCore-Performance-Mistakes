using EFCore.Performance.Api.Data;
using EFCore.Performance.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Performance.Api.Services;

/// <summary>Filtered order page. The before path filters in memory after <c>ToListAsync</c>.</summary>
public sealed class MaterializationQueryService(AppDbContext db, ILogger<MaterializationQueryService> logger)
{
    /// <summary>Loads up to <see cref="QueryLimits.MaterializationSafetyCap"/> orders, then filters in application code.</summary>
    public async Task<IReadOnlyList<FilteredOrderResponse>> GetBeforeAsync(
        OrderFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Materializing up to {Cap} orders before filtering Status={Status} PlacedAfter={PlacedAfter} in application code.",
            QueryLimits.MaterializationSafetyCap,
            filter.Status,
            filter.PlacedAfter);

        var orders = await QueryShapes.MaterializationSource(db).ToListAsync(cancellationToken);

        return orders
            .Where(order => order.Status == filter.Status && order.PlacedAt >= filter.PlacedAfter)
            .OrderBy(order => order.PlacedAt)
            .ThenBy(order => order.Id)
            .Skip(skip)
            .Take(take)
            .Select(order => new FilteredOrderResponse(
                order.Id,
                order.OrderNumber,
                order.Customer.Name,
                order.Status.ToString(),
                order.PlacedAt,
                order.TotalAmount))
            .ToList();
    }

    /// <summary>Applies the status and date filter on <c>IQueryable</c> before <c>ToListAsync</c>.</summary>
    public async Task<IReadOnlyList<FilteredOrderResponse>> GetAfterAsync(
        OrderFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Composing Status={Status} PlacedAfter={PlacedAfter} on IQueryable before materialization. Skip={Skip} Take={Take}",
            filter.Status,
            filter.PlacedAfter,
            skip,
            take);

        var rows = await QueryShapes.MaterializationComposed(db, filter, skip, take).ToListAsync(cancellationToken);
        return rows.Select(row => new FilteredOrderResponse(
                row.Id,
                row.OrderNumber,
                row.CustomerName,
                row.Status.ToString(),
                row.PlacedAt,
                row.TotalAmount))
            .ToList();
    }
}
