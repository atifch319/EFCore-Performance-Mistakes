using EFCore.Performance.Api.Entities;

namespace EFCore.Performance.Api.DTOs;

internal sealed record FilteredOrderRow(
    int Id,
    string OrderNumber,
    string CustomerName,
    OrderStatus Status,
    DateTimeOffset PlacedAt,
    decimal TotalAmount);

internal sealed record SqlInspectionRow(
    int Id,
    string OrderNumber,
    OrderStatus Status,
    DateTimeOffset PlacedAt,
    decimal TotalAmount);
