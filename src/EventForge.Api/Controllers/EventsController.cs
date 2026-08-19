using EventForge.Api.Authentication;
using EventForge.Api.Data;
using EventForge.Api.Infrastructure;
using EventForge.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EventForge.Api.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]
public sealed class EventsController(
    IMongoRepository<EventDocument> events,
    IMongoRepository<VenueDocument> venues,
    IMongoRepository<SessionDocument> sessions,
    IMongoRepository<RegistrationDocument> registrations,
    ICacheService cache) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EventDocument>>> List(
        [FromQuery] string? status = null,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = string.IsNullOrWhiteSpace(status) ? CacheKeys.EventsList : $"{CacheKeys.EventsList}:{status.ToLowerInvariant()}";
        var cached = await cache.GetAsync<List<EventDocument>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Ok(cached.Take(Math.Clamp(limit, 1, 100)));
        }

        var filter = string.IsNullOrWhiteSpace(status)
            ? Builders<EventDocument>.Filter.Empty
            : Builders<EventDocument>.Filter.Eq(item => item.Status, status);
        var result = (await events.ListAsync(
            filter,
            Builders<EventDocument>.Sort.Ascending(item => item.StartsAtUtc),
            cancellationToken)).ToList();
        await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(2), cancellationToken);
        return Ok(result.Take(Math.Clamp(limit, 1, 100)));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EventDocument>> Get(string id, CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync<EventDocument>(CacheKeys.Event(id), cancellationToken);
        if (cached is not null)
        {
            return Ok(cached);
        }

        var eventDocument = await events.FindByIdAsync(id, cancellationToken);
        if (eventDocument is null)
        {
            return NotFound();
        }

        await cache.SetAsync(CacheKeys.Event(id), eventDocument, TimeSpan.FromMinutes(2), cancellationToken);
        return Ok(eventDocument);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.Organizer)]
    public async Task<ActionResult<EventDocument>> Create(EventRequest request, CancellationToken cancellationToken)
    {
        var validation = await Validate(request, cancellationToken);
        if (validation is not null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: validation);
        }

        var eventDocument = await events.InsertAsync(new EventDocument
        {
            Name = request.Name.Trim(),
            Slug = request.Slug.Trim().ToLowerInvariant(),
            Description = request.Description.Trim(),
            VenueId = request.VenueId,
            OrganizerId = User.GetUserId()!,
            StartsAtUtc = request.StartsAtUtc.ToUniversalTime(),
            EndsAtUtc = request.EndsAtUtc.ToUniversalTime(),
            Status = request.Status,
            Tags = request.Tags.Select(tag => tag.Trim()).Where(tag => tag.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        }, cancellationToken);

        await cache.RemoveAsync(CacheKeys.EventsList, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = eventDocument.Id }, eventDocument);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.Organizer)]
    public async Task<ActionResult<EventDocument>> Update(
        string id,
        EventRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await Validate(request, cancellationToken);
        if (validation is not null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: validation);
        }

        var eventDocument = await events.FindByIdAsync(id, cancellationToken);
        if (eventDocument is null)
        {
            return NotFound();
        }

        eventDocument.Name = request.Name.Trim();
        eventDocument.Slug = request.Slug.Trim().ToLowerInvariant();
        eventDocument.Description = request.Description.Trim();
        eventDocument.VenueId = request.VenueId;
        eventDocument.StartsAtUtc = request.StartsAtUtc.ToUniversalTime();
        eventDocument.EndsAtUtc = request.EndsAtUtc.ToUniversalTime();
        eventDocument.Status = request.Status;
        eventDocument.Tags = request.Tags.Select(tag => tag.Trim()).Where(tag => tag.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        await events.ReplaceAsync(eventDocument, cancellationToken);
        await InvalidateEventCaches(eventDocument, cancellationToken);
        return Ok(eventDocument);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.Organizer)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        if (await sessions.FindOneAsync(Builders<SessionDocument>.Filter.Eq(item => item.EventId, id), cancellationToken) is not null ||
            await registrations.FindOneAsync(Builders<RegistrationDocument>.Filter.Eq(item => item.EventId, id), cancellationToken) is not null)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Event has related sessions or registrations.",
                Detail = "Remove related documents before deleting this event.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var eventDocument = await events.FindByIdAsync(id, cancellationToken);
        if (eventDocument is null || !await events.DeleteAsync(id, cancellationToken))
        {
            return NotFound();
        }

        await InvalidateEventCaches(eventDocument, cancellationToken);
        return NoContent();
    }

    private async Task<string?> Validate(EventRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Slug))
        {
            return "Event name and slug are required.";
        }

        if (request.StartsAtUtc >= request.EndsAtUtc)
        {
            return "Event end time must be after its start time.";
        }

        if (!EventStatuses.All.Contains(request.Status))
        {
            return "Unknown event status.";
        }

        return await venues.FindByIdAsync(request.VenueId, cancellationToken) is null
            ? "Referenced venue was not found."
            : null;
    }

    private async Task InvalidateEventCaches(EventDocument eventDocument, CancellationToken cancellationToken)
    {
        await cache.RemoveAsync(CacheKeys.EventsList, cancellationToken);
        await cache.RemoveAsync($"{CacheKeys.EventsList}:{eventDocument.Status.ToLowerInvariant()}", cancellationToken);
        await cache.RemoveAsync(CacheKeys.Event(eventDocument.Id), cancellationToken);
        await cache.RemoveAsync(CacheKeys.SessionsList, cancellationToken);
        await cache.RemoveAsync(CacheKeys.RegistrationsForEvent(eventDocument.Id), cancellationToken);
    }
}
