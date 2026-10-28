using EFCore.Performance.Api.DTOs;
using EFCore.Performance.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EFCore.Performance.Api.Controllers;

/// <summary>The same shipped-order filter, applied after materialization or inside SQL.</summary>
[ApiController]
[Route("api/materialization")]
public sealed class MaterializationController(MaterializationQueryService queries) : ControllerBase
{
    [HttpGet("before")]
    public async Task<ActionResult<IReadOnlyList<FilteredOrderResponse>>> Before(
        [FromQuery] int? skip,
        [FromQuery] int? take,
        [FromQuery] string? status,
        [FromQuery] DateTimeOffset? placedAfter,
        CancellationToken cancellationToken)
    {
        if (!TryReadQuery(skip, take, status, placedAfter, out var window, out var filter, out var error))
        {
            return error;
        }

        return Ok(await queries.GetBeforeAsync(filter, window.Skip, window.Take, cancellationToken));
    }

    [HttpGet("after")]
    public async Task<ActionResult<IReadOnlyList<FilteredOrderResponse>>> After(
        [FromQuery] int? skip,
        [FromQuery] int? take,
        [FromQuery] string? status,
        [FromQuery] DateTimeOffset? placedAfter,
        CancellationToken cancellationToken)
    {
        if (!TryReadQuery(skip, take, status, placedAfter, out var window, out var filter, out var error))
        {
            return error;
        }

        return Ok(await queries.GetAfterAsync(filter, window.Skip, window.Take, cancellationToken));
    }

    private bool TryReadQuery(
        int? skip,
        int? take,
        string? status,
        DateTimeOffset? placedAfter,
        out ListWindow window,
        out OrderFilter filter,
        out ActionResult error)
    {
        window = default;
        filter = default;
        error = null!;

        if (!ListWindowParser.TryParse(skip, take, out window, out var pagingError))
        {
            error = Problem(
                title: "Invalid paging parameters.",
                detail: pagingError,
                statusCode: StatusCodes.Status400BadRequest);
            return false;
        }

        if (!OrderFilterParser.TryParse(status, placedAfter, out filter, out var statusError))
        {
            error = Problem(
                title: "Invalid filter.",
                detail: statusError,
                statusCode: StatusCodes.Status400BadRequest);
            return false;
        }

        return true;
    }
}
