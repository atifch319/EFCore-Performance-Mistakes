using Microsoft.AspNetCore.Diagnostics;

namespace EFCore.Performance.Api.Infrastructure;

public static class GlobalExceptionHandler
{
    public static async Task WriteAsync(HttpContext context)
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var logger = context.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("GlobalExceptionHandler");

        logger.LogError(
            exception,
            "Unhandled exception for {Method} {Path}",
            context.Request.Method,
            context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await Results.Problem(
            title: "An unexpected error occurred.",
            detail: "The request could not be completed. See server logs for details.",
            statusCode: StatusCodes.Status500InternalServerError,
            instance: context.Request.Path).ExecuteAsync(context);
    }
}
