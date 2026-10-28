using EFCore.Performance.Api.Data;
using EFCore.Performance.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace EFCore.Performance.Tests.Unit;

public sealed class OrderIndexConfigurationTests
{
    [Fact]
    public void FilteredReadIndex_IsConfiguredOnStatusPlacedAtAndId()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=Unused;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        using var db = new AppDbContext(options);
        var index = db.Model.FindEntityType(typeof(Order))!
            .GetIndexes()
            .Single(candidate => candidate.GetDatabaseName() == "IX_Orders_Status_PlacedAt_Id");

        Assert.Equal(["Status", "PlacedAt", "Id"], index.Properties.Select(property => property.Name).ToArray());
    }
}
