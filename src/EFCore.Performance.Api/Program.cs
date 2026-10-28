using EFCore.Performance.Api.Data;
using EFCore.Performance.Api.Infrastructure;
using EFCore.Performance.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Connection string 'DefaultConnection' is not configured. Set ConnectionStrings:DefaultConnection in appsettings, user secrets, or the ConnectionStrings__DefaultConnection environment variable.");
    }

    options.UseSqlServer(connectionString);
});

builder.Services.AddScoped<TrackingQueryService>();
builder.Services.AddScoped<NPlusOneQueryService>();
builder.Services.AddScoped<MaterializationQueryService>();
builder.Services.AddScoped<ProjectionQueryService>();
builder.Services.AddScoped<SqlInspectionQueryService>();

var app = builder.Build();

app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(GlobalExceptionHandler.WriteAsync);
});

app.MapControllers();

await DatabaseInitializer.InitializeAsync(app);

app.Run();

public partial class Program;
