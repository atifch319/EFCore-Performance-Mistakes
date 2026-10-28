using EFCore.Performance.Api.DTOs;
using EFCore.Performance.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EFCore.Performance.Api.Controllers;

/// <summary>Order summaries that show a per-order query loop beside one projected query.</summary>
[ApiController]
[Route("api/nplusone")]
public sealed class NPlusOneController(NPlusOneQueryService queries) : ControllerBase
{
    [HttpGet("before")]
    public async Task<ActionResult<IReadOnlyList<NPlusOneOrderResponse>>> Before(
        [FromQuery] int? skip,
        [FromQuery] int? take,
        CancellationToken cancellationToken)
    {
        if (ValidateWindow(skip, take, out var window) is { } error)
        {
            return error;
        }

        return Ok(await queries.GetBeforeAsync(window.Skip, window.Take, cancellationToken));
    }

    [HttpGet("after")]
    public async Task<ActionResult<IReadOnlyList<NPlusOneOrderResponse>>> After(
        [FromQuery] int? skip,
        [FromQuery] int? take,
        CancellationToken cancellationToken)
    {
        if (ValidateWindow(skip, take, out var window) is { } error)
        {
            return error;
        }

        return Ok(await queries.GetAfterAsync(window.Skip, window.Take, cancellationToken));
    }

    private ActionResult? ValidateWindow(int? skip, int? take, out ListWindow window)
    {
        if (ListWindowParser.TryParse(skip, take, out window, out var error))
        {
            return null;
        }

        return Problem(
            title: "Invalid paging parameters.",
            detail: error,
            statusCode: StatusCodes.Status400BadRequest);
    }
}
