using Microsoft.Data.SqlClient;

namespace EFCore.Performance.Tests.Support;

public static class SqlServerAvailability
{
    public const string SkipReason =
        "SQL Server is not reachable. This skipped test is not a passing SQL Server verification. Start the instance or set EFCORE_SQL_SERVER / EFCORE_TEST_CONNECTION.";

    private static readonly Lazy<bool> Reachable = new(Probe);

    public static bool IsAvailable => Reachable.Value;

    public static string Server =>
        Environment.GetEnvironmentVariable("EFCORE_SQL_SERVER") is { Length: > 0 } server
            ? server
            : "localhost";

    public static string TestConnectionString => ResolveTestConnectionString();

    private static bool Probe()
    {
        try
        {
            using var connection = new SqlConnection(MasterConnectionString());
            connection.Open();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static string MasterConnectionString()
    {
        var configured = Environment.GetEnvironmentVariable("EFCORE_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(configured))
        {
            return $"Server={Server};Database=master;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=3";
        }

        var builder = new SqlConnectionStringBuilder(configured)
        {
            InitialCatalog = "master",
            ConnectTimeout = 3
        };
        return builder.ConnectionString;
    }

    private static string ResolveTestConnectionString()
    {
        var configured = Environment.GetEnvironmentVariable("EFCORE_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(configured))
        {
            return $"Server={Server};Database=EFCorePerformanceTests;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=30";
        }

        var builder = new SqlConnectionStringBuilder(configured);
        if (!builder.InitialCatalog.Contains("Test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "EFCORE_TEST_CONNECTION must target a database whose name contains 'Test'. Integration tests migrate and seed that database.");
        }

        return builder.ConnectionString;
    }
}

public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute()
    {
        if (!SqlServerAvailability.IsAvailable)
        {
            Skip = SqlServerAvailability.SkipReason;
        }
    }
}
