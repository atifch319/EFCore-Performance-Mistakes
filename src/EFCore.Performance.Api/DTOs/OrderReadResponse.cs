namespace EFCore.Performance.Api.DTOs;

public sealed record OrderReadResponse(
    int Id,
    string OrderNumber,
    int CustomerId,
    string CustomerName,
    string Status,
    DateTimeOffset PlacedAt,
    decimal TotalAmount);
