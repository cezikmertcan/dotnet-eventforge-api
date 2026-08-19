using EventForge.Api.Data;
using EventForge.Api.Infrastructure;
using EventForge.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EventForge.Api.Controllers;

[ApiController]
[Route("api/venues")]
[Authorize]
public sealed class VenuesController(
    IMongoRepository<VenueDocument> venues,
    IMongoRepository<EventDocument> events,
    ICacheService cache) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VenueDocument>>> List(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var cached = await cache.GetAsync<List<VenueDocument>>(CacheKeys.VenuesList, cancellationToken);
        if (cached is not null)
        {
            return Ok(cached.Take(Math.Clamp(limit, 1, 100)));
        }

        var result = (await venues.ListAsync(
            sort: Builders<VenueDocument>.Sort.Ascending(venue => venue.City),
            cancellationToken: cancellationToken)).ToList();
        await cache.SetAsync(CacheKeys.VenuesList, result, TimeSpan.FromMinutes(5), cancellationToken);
        return Ok(result.Take(Math.Clamp(limit, 1, 100)));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<VenueDocument>> Get(string id, CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync<VenueDocument>(CacheKeys.Venue(id), cancellationToken);
        if (cached is not null)
        {
            return Ok(cached);
        }

        var venue = await venues.FindByIdAsync(id, cancellationToken);
        if (venue is null)
        {
            return NotFound();
        }

        await cache.SetAsync(CacheKeys.Venue(id), venue, TimeSpan.FromMinutes(5), cancellationToken);
        return Ok(venue);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.Organizer)]
    public async Task<ActionResult<VenueDocument>> Create(
        VenueRequest request,
        CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (validation is not null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: validation);
        }

        var venue = await venues.InsertAsync(new VenueDocument
        {
            Name = request.Name.Trim(),
            Address = request.Address.Trim(),
            City = request.City.Trim(),
            Country = request.Country.Trim(),
            Capacity = request.Capacity,
            IsActive = request.IsActive
        }, cancellationToken);

        await cache.RemoveAsync(CacheKeys.VenuesList, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = venue.Id }, venue);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.Organizer)]
    public async Task<ActionResult<VenueDocument>> Update(
        string id,
        VenueRequest request,
        CancellationToken cancellationToken)
    {
        var validation = Validate(request);
        if (validation is not null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: validation);
        }

        var venue = await venues.FindByIdAsync(id, cancellationToken);
        if (venue is null)
        {
            return NotFound();
        }

        venue.Name = request.Name.Trim();
        venue.Address = request.Address.Trim();
        venue.City = request.City.Trim();
        venue.Country = request.Country.Trim();
        venue.Capacity = request.Capacity;
        venue.IsActive = request.IsActive;
        await venues.ReplaceAsync(venue, cancellationToken);
        await cache.RemoveAsync(CacheKeys.VenuesList, cancellationToken);
        await cache.RemoveAsync(CacheKeys.Venue(id), cancellationToken);
        return Ok(venue);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.Organizer)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        if (await events.FindOneAsync(Builders<EventDocument>.Filter.Eq(item => item.VenueId, id), cancellationToken) is not null)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Venue is referenced by an event.",
                Detail = "Move the event to another venue before deleting this document.",
                Status = StatusCodes.Status409Conflict
            });
        }

        if (!await venues.DeleteAsync(id, cancellationToken))
        {
            return NotFound();
        }

        await cache.RemoveAsync(CacheKeys.VenuesList, cancellationToken);
        await cache.RemoveAsync(CacheKeys.Venue(id), cancellationToken);
        return NoContent();
    }

    private static string? Validate(VenueRequest request)
        => string.IsNullOrWhiteSpace(request.Name) ? "Venue name is required."
            : request.Capacity is < 1 or > 1_000_000 ? "Capacity must be between 1 and 1,000,000."
            : string.IsNullOrWhiteSpace(request.City) ? "City is required."
            : null;
}
