namespace EFCore.Performance.Api.Data;

public static class DemoSeed
{
    public const int ProductCount = 12;
    public const int CustomerCount = 20;
    public const int OrdersPerCustomer = 5;
    public const int ItemsPerOrder = 3;

    public static int OrderCount => CustomerCount * OrdersPerCustomer;

    public static int OrderItemCount => OrderCount * ItemsPerOrder;

    public static DateTimeOffset CustomerCreatedAt(int customerNumber) =>
        new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(customerNumber);

    public static DateTimeOffset OrderPlacedAt(int sequence) =>
        new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero).AddDays(sequence);
}
