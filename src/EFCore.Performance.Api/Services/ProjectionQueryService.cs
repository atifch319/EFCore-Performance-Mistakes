using EFCore.Performance.Api.Data;
using EFCore.Performance.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Performance.Api.Services;

/// <summary>Narrow order DTO. The before path still materializes full order and customer rows.</summary>
public sealed class ProjectionQueryService(AppDbContext db, ILogger<ProjectionQueryService> logger)
{
    /// <summary>Loads complete order and customer entities, then copies five fields into the DTO.</summary>
    public async Task<IReadOnlyList<ProjectedOrderResponse>> GetBeforeAsync(
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Materializing full order and customer entities for a narrow response. Skip={Skip} Take={Take}",
            skip,
            take);

        var orders = await QueryShapes.ProjectionEntities(db, skip, take).ToListAsync(cancellationToken);
        return orders.Select(order => new ProjectedOrderResponse(
                order.Id,
                order.OrderNumber,
                order.Customer.Name,
                order.PlacedAt,
                order.TotalAmount))
            .ToList();
    }

    /// <summary>Projects only the DTO columns in SQL.</summary>
    public async Task<IReadOnlyList<ProjectedOrderResponse>> GetAfterAsync(
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Projecting only the response columns. Skip={Skip} Take={Take}",
            skip,
            take);

        return await QueryShapes.ProjectionSelected(db, skip, take).ToListAsync(cancellationToken);
    }
}
