using EFCore.Performance.Api.DTOs;
using EFCore.Performance.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace EFCore.Performance.Api.Controllers;

/// <summary>Read-only order queries that differ only by change tracking.</summary>
[ApiController]
[Route("api/tracking")]
public sealed class TrackingController(TrackingQueryService queries) : ControllerBase
{
    [HttpGet("before")]
    public async Task<ActionResult<IReadOnlyList<OrderReadResponse>>> Before(
        [FromQuery] int? skip,
        [FromQuery] int? take,
        CancellationToken cancellationToken)
    {
        if (ValidateWindow(skip, take, out var window) is { } error)
        {
            return error;
        }

        return Ok(await queries.GetTrackedAsync(window.Skip, window.Take, cancellationToken));
    }

    [HttpGet("after")]
    public async Task<ActionResult<IReadOnlyList<OrderReadResponse>>> After(
        [FromQuery] int? skip,
        [FromQuery] int? take,
        CancellationToken cancellationToken)
    {
        if (ValidateWindow(skip, take, out var window) is { } error)
        {
            return error;
        }

        return Ok(await queries.GetUntrackedAsync(window.Skip, window.Take, cancellationToken));
    }

    [HttpGet("orders/{id:int}")]
    public async Task<ActionResult<OrderReadResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return Problem(
                title: "Invalid order id.",
                detail: "The order id must be a positive integer.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var order = await queries.FindAsync(id, cancellationToken);
        if (order is null)
        {
            return Problem(
                title: "Order not found.",
                detail: "No order exists with the supplied identifier.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(order);
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
