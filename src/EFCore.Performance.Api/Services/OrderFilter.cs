using EFCore.Performance.Api.Entities;

namespace EFCore.Performance.Api.Services;

public readonly record struct OrderFilter(OrderStatus Status, DateTimeOffset PlacedAfter);

public static class OrderFilterParser
{
    public static readonly DateTimeOffset DefaultPlacedAfter =
        new(2024, 2, 1, 0, 0, 0, TimeSpan.Zero);

    public static bool TryParse(string? status, DateTimeOffset? placedAfter, out OrderFilter filter, out string? error)
    {
        var statusText = string.IsNullOrWhiteSpace(status) ? nameof(OrderStatus.Shipped) : status.Trim();
        if (int.TryParse(statusText, out _)
            || !Enum.TryParse(statusText, ignoreCase: true, out OrderStatus parsed)
            || !Enum.IsDefined(parsed))
        {
            filter = default;
            error = "status must be one of Pending, Paid, Shipped, or Cancelled.";
            return false;
        }

        filter = new OrderFilter(parsed, placedAfter ?? DefaultPlacedAfter);
        error = null;
        return true;
    }
}
