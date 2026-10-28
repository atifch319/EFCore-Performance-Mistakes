using EFCore.Performance.Tests.Support;

namespace EFCore.Performance.Tests.Integration;

public sealed class DiagnosticsExposureTests
{
    [Fact]
    public async Task SqlDiagnostics_AreNotExposed_WhenTheHostIsProduction()
    {
        await using var factory = new ProductionApiFactory();
        var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/sql-inspection/diagnostics?take=5&status=Shipped");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        Assert.DoesNotContain("SELECT", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InternalNotes", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("IX_Orders_Status_PlacedAt_Id", body, StringComparison.Ordinal);
    }
}
