using EFCore.Performance.Api.DTOs;
using EFCore.Performance.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EFCore.Performance.Api.Controllers;

/// <summary>The same narrow DTO, loaded from full entities or from a column projection.</summary>
[ApiController]
[Route("api/projection")]
public sealed class ProjectionController(ProjectionQueryService queries) : ControllerBase
{
    [HttpGet("before")]
    public async Task<ActionResult<IReadOnlyList<ProjectedOrderResponse>>> Before(
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
    public async Task<ActionResult<IReadOnlyList<ProjectedOrderResponse>>> After(
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
