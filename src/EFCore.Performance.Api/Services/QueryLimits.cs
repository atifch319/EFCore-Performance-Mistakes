namespace EFCore.Performance.Api.Services;

public static class QueryLimits
{
    public const int DefaultTake = 20;

    public const int MaxTake = 50;

    public const int MaxSkip = 1_000;

    // The demo seed inserts 100 orders. This cap keeps the deliberately inefficient
    // paths from reading an unbounded table if the database is later loaded with more rows.
    // Once the table grows past the cap, those paths can disagree with the composed queries.
    public const int MaterializationSafetyCap = 500;
}
