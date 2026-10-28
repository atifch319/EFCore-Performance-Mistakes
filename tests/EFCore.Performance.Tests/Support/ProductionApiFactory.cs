using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace EFCore.Performance.Tests.Support;

public sealed class ProductionApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Production);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=127.0.0.1,1;Database=EFCorePerformanceTests;Trusted_Connection=True;TrustServerCertificate=True;Connect Timeout=1",
                ["Database:SeedOnStartup"] = "false",
                ["Database:ApplyMigrationsOnStartup"] = "false"
            });
        });
    }
}
