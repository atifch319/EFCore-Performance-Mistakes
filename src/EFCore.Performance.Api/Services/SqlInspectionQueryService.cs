using EFCore.Performance.Api.Data;
using EFCore.Performance.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Performance.Api.Services;

/// <summary>Filtered order rows, plus a Development-only <c>ToQueryString()</c> diagnostic.</summary>
public sealed class SqlInspectionQueryService(AppDbContext db, ILogger<SqlInspectionQueryService> logger)
{
    public const string IndexNote =
        "IX_Orders_Status_PlacedAt_Id is a nonclustered index on (Status, PlacedAt, Id). " +
        "It matches the filtered query's equality, range, and sort. SQL Server may still scan when the demo table is small or when statistics favor another plan. " +
        "These statements come from ToQueryString() and are returned only when the host environment is Development.";

    /// <summary>Loads up to <see cref="QueryLimits.MaterializationSafetyCap"/> full order rows, then filters in memory.</summary>
    public async Task<IReadOnlyList<SqlInspectionOrderResponse>> GetBeforeAsync(
        OrderFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Loading up to {Cap} full order rows, then filtering Status={Status} in application memory.",
            QueryLimits.MaterializationSafetyCap,
            filter.Status);

        var orders = await QueryShapes.SqlInspectionSource(db).ToListAsync(cancellationToken);
        return orders
            .Where(order => order.Status == filter.Status && order.PlacedAt >= filter.PlacedAfter)
            .OrderBy(order => order.PlacedAt)
            .ThenBy(order => order.Id)
            .Skip(skip)
            .Take(take)
            .Select(order => new SqlInspectionOrderResponse(
                order.Id,
                order.OrderNumber,
                order.Status.ToString(),
                order.PlacedAt,
                order.TotalAmount))
            .ToList();
    }

    /// <summary>Filters, sorts, pages, and projects in one translated query.</summary>
    public async Task<IReadOnlyList<SqlInspectionOrderResponse>> GetAfterAsync(
        OrderFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Executing the filtered projection for Status={Status} PlacedAfter={PlacedAfter}. Skip={Skip} Take={Take}",
            filter.Status,
            filter.PlacedAfter,
            skip,
            take);

        var rows = await QueryShapes.SqlInspectionComposed(db, filter, skip, take).ToListAsync(cancellationToken);
        return rows.Select(row => new SqlInspectionOrderResponse(
                row.Id,
                row.OrderNumber,
                row.Status.ToString(),
                row.PlacedAt,
                row.TotalAmount))
            .ToList();
    }

    /// <summary>Returns <c>ToQueryString()</c> for the demonstration queries. The controller exposes this only in Development.</summary>
    public SqlDiagnosticsResponse GetDiagnostics(OrderFilter filter, int skip, int take)
    {
        logger.LogInformation("Generating development SQL diagnostics with ToQueryString for Skip={Skip} Take={Take}", skip, take);

        return new SqlDiagnosticsResponse(
            QueryShapes.ProjectionEntities(db, skip, take).ToQueryString(),
            QueryShapes.ProjectionSelected(db, skip, take).ToQueryString(),
            QueryShapes.MaterializationSource(db).ToQueryString(),
            QueryShapes.MaterializationComposed(db, filter, skip, take).ToQueryString(),
            QueryShapes.SqlInspectionSource(db).ToQueryString(),
            QueryShapes.SqlInspectionComposed(db, filter, skip, take).ToQueryString(),
            QueryShapes.NPlusOneProjected(db, skip, take).ToQueryString(),
            IndexNote);
    }
}
