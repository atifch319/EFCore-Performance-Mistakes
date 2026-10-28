using EFCore.Performance.Api.Data;
using EFCore.Performance.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Performance.Api.Services;

/// <summary>Order summaries loaded with one extra customer query and one extra item-count query per order.</summary>
public sealed class NPlusOneQueryService(AppDbContext db, ILogger<NPlusOneQueryService> logger)
{
    /// <summary>Page of orders, then a customer-name query and an item-count query inside the loop.</summary>
    public async Task<IReadOnlyList<NPlusOneOrderResponse>> GetBeforeAsync(
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Loading an order page, then querying the customer and item count once per order. Skip={Skip} Take={Take}",
            skip,
            take);

        var orders = await QueryShapes.NPlusOneOrderPage(db, skip, take).ToListAsync(cancellationToken);
        var results = new List<NPlusOneOrderResponse>(orders.Count);

        // Intentional round trips. Lazy-loading proxies are not involved.
        foreach (var order in orders)
        {
            var customerName = await db.Customers
                .AsNoTracking()
                .Where(customer => customer.Id == order.CustomerId)
                .Select(customer => customer.Name)
                .FirstAsync(cancellationToken);

            var itemCount = await db.OrderItems
                .AsNoTracking()
                .CountAsync(item => item.OrderId == order.Id, cancellationToken);

            results.Add(new NPlusOneOrderResponse(
                order.Id,
                order.OrderNumber,
                customerName,
                itemCount,
                order.TotalAmount));
        }

        return results;
    }

    /// <summary>Same summary as <see cref="GetBeforeAsync"/>, projected in one query with a <c>COUNT(*)</c> subquery.</summary>
    public async Task<IReadOnlyList<NPlusOneOrderResponse>> GetAfterAsync(
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Loading order summaries with one projected query. Skip={Skip} Take={Take}",
            skip,
            take);

        return await QueryShapes.NPlusOneProjected(db, skip, take).ToListAsync(cancellationToken);
    }
}
