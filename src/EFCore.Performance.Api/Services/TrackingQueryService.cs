using EFCore.Performance.Api.Data;
using EFCore.Performance.Api.DTOs;
using EFCore.Performance.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Performance.Api.Services;

/// <summary>Read-only order page that still materializes tracked entities.</summary>
public sealed class TrackingQueryService(AppDbContext db, ILogger<TrackingQueryService> logger)
{
    /// <summary>Loads the same page as <see cref="GetUntrackedAsync"/> while change tracking is on.</summary>
    public async Task<IReadOnlyList<OrderReadResponse>> GetTrackedAsync(
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Read-only order query materializing tracked entities. Skip={Skip} Take={Take}",
            skip,
            take);

        var orders = await QueryShapes.TrackedOrders(db, skip, take).ToListAsync(cancellationToken);
        return orders.Select(Map).ToList();
    }

    /// <summary>Loads the same page as <see cref="GetTrackedAsync"/> with <c>AsNoTracking()</c>.</summary>
    public async Task<IReadOnlyList<OrderReadResponse>> GetUntrackedAsync(
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Read-only order query materializing entities without change tracking. Skip={Skip} Take={Take}",
            skip,
            take);

        var orders = await QueryShapes.UntrackedOrders(db, skip, take).ToListAsync(cancellationToken);
        return orders.Select(Map).ToList();
    }

    public async Task<OrderReadResponse?> FindAsync(int id, CancellationToken cancellationToken)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Include(candidate => candidate.Customer)
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        return order is null ? null : Map(order);
    }

    private static OrderReadResponse Map(Order order) =>
        new(
            order.Id,
            order.OrderNumber,
            order.CustomerId,
            order.Customer.Name,
            order.Status.ToString(),
            order.PlacedAt,
            order.TotalAmount);
}
