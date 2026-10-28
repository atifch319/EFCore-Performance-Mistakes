namespace EFCore.Performance.Api.DTOs;

public sealed record FilteredOrderResponse(
    int Id,
    string OrderNumber,
    string CustomerName,
    string Status,
    DateTimeOffset PlacedAt,
    decimal TotalAmount);
