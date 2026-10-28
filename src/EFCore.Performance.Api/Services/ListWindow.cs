namespace EFCore.Performance.Api.Services;

public readonly record struct ListWindow(int Skip, int Take);

public static class ListWindowParser
{
    public static bool TryParse(int? skip, int? take, out ListWindow window, out string? error)
    {
        var normalizedSkip = skip ?? 0;
        var normalizedTake = take ?? QueryLimits.DefaultTake;

        if (normalizedSkip < 0 || normalizedSkip > QueryLimits.MaxSkip)
        {
            window = default;
            error = $"skip must be between 0 and {QueryLimits.MaxSkip}.";
            return false;
        }

        if (normalizedTake < 1 || normalizedTake > QueryLimits.MaxTake)
        {
            window = default;
            error = $"take must be between 1 and {QueryLimits.MaxTake}.";
            return false;
        }

        window = new ListWindow(normalizedSkip, normalizedTake);
        error = null;
        return true;
    }
}
