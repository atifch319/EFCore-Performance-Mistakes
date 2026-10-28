using EFCore.Performance.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EFCore.Performance.Tests.Support;

public sealed class SqlServerApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public CountingCommandInterceptor Interceptor { get; } = new();

    public async Task InitializeAsync()
    {
        var expectedDatabase = new SqlConnectionStringBuilder(SqlServerAvailability.TestConnectionString).InitialCatalog;
        if (!expectedDatabase.Contains("Test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Refusing to use non-test database '{expectedDatabase}'.");
        }

        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var actualDatabase = db.Database.GetDbConnection().Database;
        if (!string.Equals(actualDatabase, expectedDatabase, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"The test host is pointed at '{actualDatabase}' instead of '{expectedDatabase}'.");
        }

        await db.Database.MigrateAsync();
        await DatabaseSeeder.SeedAsync(db);
        Interceptor.Reset();
    }

    async Task IAsyncLifetime.DisposeAsync() => await DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = SqlServerAvailability.TestConnectionString,
                ["Database:SeedOnStartup"] = "false",
                ["Database:ApplyMigrationsOnStartup"] = "false"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            TestDbContextRegistration.Replace(services, options =>
            {
                options.UseSqlServer(SqlServerAvailability.TestConnectionString);
                options.AddInterceptors(Interceptor);
            });
        });
    }
}

[CollectionDefinition(Name)]
public sealed class SqlServerApiCollection : ICollectionFixture<SqlServerApiFactory>
{
    public const string Name = "SqlServerApi";
}
