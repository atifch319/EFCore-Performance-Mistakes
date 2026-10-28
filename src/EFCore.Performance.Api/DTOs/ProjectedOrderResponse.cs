namespace EFCore.Performance.Api.DTOs;

public sealed record ProjectedOrderResponse(
    int Id,
    string OrderNumber,
    string CustomerName,
    DateTimeOffset PlacedAt,
    decimal TotalAmount);
