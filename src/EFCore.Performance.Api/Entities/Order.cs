namespace EFCore.Performance.Api.Entities;

public sealed class Order
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public OrderStatus Status { get; set; }

    public DateTimeOffset PlacedAt { get; set; }

    public decimal TotalAmount { get; set; }

    public string ShippingAddress { get; set; } = string.Empty;

    public string InternalNotes { get; set; } = string.Empty;

    public Customer Customer { get; set; } = null!;

    public List<OrderItem> Items { get; set; } = [];
}
