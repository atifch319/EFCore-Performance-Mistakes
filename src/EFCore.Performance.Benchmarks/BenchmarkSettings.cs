namespace EFCore.Performance.Benchmarks;

internal static class BenchmarkSettings
{
    public const string DatabaseName = "EFCorePerformanceBenchmarks";

    public const int ProductCount = 20;

    public const int CustomerCount = 100;

    public const int OrdersPerCustomer = 20;

    public const int ItemsPerOrder = 2;

    public const int QueryTake = 50;

    public static int OrderCount => CustomerCount * OrdersPerCustomer;
}
