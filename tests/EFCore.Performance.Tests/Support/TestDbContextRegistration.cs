using EFCore.Performance.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EFCore.Performance.Tests.Support;

internal static class TestDbContextRegistration
{
    public static void Replace(IServiceCollection services, Action<DbContextOptionsBuilder> configure)
    {
        for (var index = services.Count - 1; index >= 0; index--)
        {
            var serviceType = services[index].ServiceType;
            if (serviceType == typeof(AppDbContext)
                || serviceType == typeof(DbContextOptions)
                || serviceType == typeof(DbContextOptions<AppDbContext>)
                || IsOptionsConfiguration(serviceType))
            {
                services.RemoveAt(index);
            }
        }

        services.AddDbContext<AppDbContext>(configure);
    }

    private static bool IsOptionsConfiguration(Type serviceType) =>
        serviceType.IsGenericType
        && serviceType.GenericTypeArguments.Length == 1
        && serviceType.GenericTypeArguments[0] == typeof(AppDbContext)
        && serviceType.Name.StartsWith("IDbContextOptionsConfiguration", StringComparison.Ordinal);
}
