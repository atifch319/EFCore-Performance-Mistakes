using EFCore.Performance.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Performance.Api.Infrastructure;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(WebApplication app, CancellationToken cancellationToken = default)
    {
        if (IsTestHost())
        {
            // WebApplicationFactory loads the API in-process. The test fixture owns migration and seeding
            // so startup cannot write to the developer's demo database.
            return;
        }

        var applyMigrations = app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", false);
        var seed = app.Configuration.GetValue("Database:SeedOnStartup", false);
        if (!applyMigrations && !seed)
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitialization");

        if (applyMigrations)
        {
            logger.LogInformation("Applying EF Core migrations.");
            await db.Database.MigrateAsync(cancellationToken);
        }

        if (seed)
        {
            try
            {
                var result = await DatabaseSeeder.SeedAsync(db, cancellationToken);
                logger.LogInformation(
                    "Demo seed finished. Inserted={Inserted} Customers={Customers} Orders={Orders} OrderItems={OrderItems} Products={Products}",
                    result.Inserted,
                    result.Customers,
                    result.Orders,
                    result.OrderItems,
                    result.Products);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Demo seed failed. Apply migrations with 'dotnet ef database update --project src/EFCore.Performance.Api' before starting the API.");
                throw;
            }
        }
    }

    private static bool IsTestHost() =>
        AppDomain.CurrentDomain.GetAssemblies().Any(static assembly =>
            string.Equals(assembly.GetName().Name, "Microsoft.AspNetCore.Mvc.Testing", StringComparison.Ordinal));
}
