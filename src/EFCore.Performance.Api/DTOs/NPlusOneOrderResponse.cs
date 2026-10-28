namespace EFCore.Performance.Api.DTOs;

public sealed record NPlusOneOrderResponse(
    int Id,
    string OrderNumber,
    string CustomerName,
    int ItemCount,
    decimal TotalAmount);
