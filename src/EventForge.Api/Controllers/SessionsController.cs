using EventForge.Api.Data;
using EventForge.Api.Infrastructure;
using EventForge.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EventForge.Api.Controllers;

[ApiController]
[Route("api/sessions")]
[Authorize]
public sealed class SessionsController(
    IMongoRepository<SessionDocument> sessions,
    IMongoRepository<EventDocument> events,
    IMongoRepository<SpeakerDocument> speakers,
    ICacheService cache) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SessionDocument>>> List(
        [FromQuery] string? eventId = null,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = string.IsNullOrWhiteSpace(eventId) ? CacheKeys.SessionsList : $"{CacheKeys.SessionsList}:event:{eventId}";
        var cached = await cache.GetAsync<List<SessionDocument>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Ok(cached.Take(Math.Clamp(limit, 1, 200)));
        }

        var filter = string.IsNullOrWhiteSpace(eventId)
            ? Builders<SessionDocument>.Filter.Empty
            : Builders<SessionDocument>.Filter.Eq(item => item.EventId, eventId);
        var result = (await sessions.ListAsync(
            filter,
            Builders<SessionDocument>.Sort.Ascending(item => item.StartsAtUtc),
            cancellationToken)).ToList();
        await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(2), cancellationToken);
        return Ok(result.Take(Math.Clamp(limit, 1, 200)));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SessionDocument>> Get(string id, CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync<SessionDocument>(CacheKeys.Session(id), cancellationToken);
        if (cached is not null)
        {
            return Ok(cached);
        }

        var session = await sessions.FindByIdAsync(id, cancellationToken);
        if (session is null)
        {
            return NotFound();
        }

        await cache.SetAsync(CacheKeys.Session(id), session, TimeSpan.FromMinutes(2), cancellationToken);
        return Ok(session);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.Organizer)]
    public async Task<ActionResult<SessionDocument>> Create(SessionRequest request, CancellationToken cancellationToken)
    {
        var validation = await Validate(request, cancellationToken);
        if (validation is not null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: validation);
        }

        var session = await sessions.InsertAsync(ToDocument(request), cancellationToken);
        await cache.RemoveAsync(CacheKeys.SessionsList, cancellationToken);
        await cache.RemoveAsync($"{CacheKeys.SessionsList}:event:{session.EventId}", cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = session.Id }, session);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.Organizer)]
    public async Task<ActionResult<SessionDocument>> Update(
        string id,
        SessionRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await Validate(request, cancellationToken);
        if (validation is not null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: validation);
        }

        var session = await sessions.FindByIdAsync(id, cancellationToken);
        if (session is null)
        {
            return NotFound();
        }

        var oldEventId = session.EventId;
        session.EventId = request.EventId;
        session.Title = request.Title.Trim();
        session.Abstract = request.Abstract.Trim();
        session.Track = request.Track.Trim();
        session.Room = request.Room.Trim();
        session.StartsAtUtc = request.StartsAtUtc.ToUniversalTime();
        session.EndsAtUtc = request.EndsAtUtc.ToUniversalTime();
        session.SpeakerIds = request.SpeakerIds.Distinct(StringComparer.Ordinal).ToList();
        await sessions.ReplaceAsync(session, cancellationToken);
        await cache.RemoveAsync(CacheKeys.SessionsList, cancellationToken);
        await cache.RemoveAsync($"{CacheKeys.SessionsList}:event:{oldEventId}", cancellationToken);
        await cache.RemoveAsync($"{CacheKeys.SessionsList}:event:{session.EventId}", cancellationToken);
        await cache.RemoveAsync(CacheKeys.Session(id), cancellationToken);
        return Ok(session);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.Organizer)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var session = await sessions.FindByIdAsync(id, cancellationToken);
        if (session is null || !await sessions.DeleteAsync(id, cancellationToken))
        {
            return NotFound();
        }

        await cache.RemoveAsync(CacheKeys.SessionsList, cancellationToken);
        await cache.RemoveAsync($"{CacheKeys.SessionsList}:event:{session.EventId}", cancellationToken);
        await cache.RemoveAsync(CacheKeys.Session(id), cancellationToken);
        return NoContent();
    }

    private async Task<string?> Validate(SessionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.EventId) || string.IsNullOrWhiteSpace(request.Title))
        {
            return "Event ID and session title are required.";
        }

        if (request.StartsAtUtc >= request.EndsAtUtc)
        {
            return "Session end time must be after its start time.";
        }

        if (await events.FindByIdAsync(request.EventId, cancellationToken) is null)
        {
            return "Referenced event was not found.";
        }

        var speakerIds = request.SpeakerIds.Distinct(StringComparer.Ordinal).ToArray();
        foreach (var speakerId in speakerIds)
        {
            if (await speakers.FindByIdAsync(speakerId, cancellationToken) is null)
            {
                return $"Referenced speaker '{speakerId}' was not found.";
            }
        }

        return null;
    }

    private static SessionDocument ToDocument(SessionRequest request) => new()
    {
        EventId = request.EventId,
        Title = request.Title.Trim(),
        Abstract = request.Abstract.Trim(),
        Track = request.Track.Trim(),
        Room = request.Room.Trim(),
        StartsAtUtc = request.StartsAtUtc.ToUniversalTime(),
        EndsAtUtc = request.EndsAtUtc.ToUniversalTime(),
        SpeakerIds = request.SpeakerIds.Distinct(StringComparer.Ordinal).ToList()
    };
}
