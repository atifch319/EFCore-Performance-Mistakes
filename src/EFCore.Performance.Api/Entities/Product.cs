namespace EFCore.Performance.Api.Entities;

public sealed class Product
{
    public int Id { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal ListPrice { get; set; }

    public bool IsActive { get; set; }

    public List<OrderItem> OrderItems { get; set; } = [];
}
