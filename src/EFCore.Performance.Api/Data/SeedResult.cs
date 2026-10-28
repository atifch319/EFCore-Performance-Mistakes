namespace EFCore.Performance.Api.Data;

public sealed record SeedResult(bool Inserted, int Customers, int Orders, int OrderItems, int Products);
