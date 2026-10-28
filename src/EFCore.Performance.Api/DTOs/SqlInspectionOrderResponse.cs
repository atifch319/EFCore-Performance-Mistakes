namespace EFCore.Performance.Api.DTOs;

public sealed record SqlInspectionOrderResponse(
    int Id,
    string OrderNumber,
    string Status,
    DateTimeOffset PlacedAt,
    decimal TotalAmount);
