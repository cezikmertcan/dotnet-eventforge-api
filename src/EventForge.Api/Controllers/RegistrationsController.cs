using EventForge.Api.Authentication;
using EventForge.Api.Data;
using EventForge.Api.Infrastructure;
using EventForge.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EventForge.Api.Controllers;

[ApiController]
[Route("api/registrations")]
[Authorize]
public sealed class RegistrationsController(
    IMongoRepository<RegistrationDocument> registrations,
    IMongoRepository<EventDocument> events,
    ICacheService cache) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RegistrationDocument>>> List(
        [FromQuery] string? eventId = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId()!;
        var isStaff = User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.Organizer);
        var cacheKey = isStaff && !string.IsNullOrWhiteSpace(eventId)
            ? CacheKeys.RegistrationsForEvent(eventId)
            : CacheKeys.RegistrationsForUser(userId);
        var cached = await cache.GetAsync<List<RegistrationDocument>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Ok(cached);
        }

        var filter = isStaff && !string.IsNullOrWhiteSpace(eventId)
            ? Builders<RegistrationDocument>.Filter.Eq(item => item.EventId, eventId)
            : Builders<RegistrationDocument>.Filter.Eq(item => item.AttendeeId, userId);
        var result = (await registrations.ListAsync(
            filter,
            Builders<RegistrationDocument>.Sort.Descending(item => item.RegisteredAtUtc),
            cancellationToken)).ToList();
        await cache.SetAsync(cacheKey, result, TimeSpan.FromSeconds(45), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RegistrationDocument>> Get(string id, CancellationToken cancellationToken)
    {
        var registration = await registrations.FindByIdAsync(id, cancellationToken);
        if (registration is null || !CanAccess(registration))
        {
            return NotFound();
        }

        return Ok(registration);
    }

    [HttpPost]
    public async Task<ActionResult<RegistrationDocument>> Create(
        RegistrationRequest request,
        CancellationToken cancellationToken)
    {
        if (await events.FindByIdAsync(request.EventId, cancellationToken) is null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Referenced event was not found.");
        }

        var attendeeId = User.GetUserId()!;
        var duplicate = await registrations.FindOneAsync(
            Builders<RegistrationDocument>.Filter.And(
                Builders<RegistrationDocument>.Filter.Eq(item => item.EventId, request.EventId),
                Builders<RegistrationDocument>.Filter.Eq(item => item.AttendeeId, attendeeId)),
            cancellationToken);
        if (duplicate is not null)
        {
            return Conflict(new ProblemDetails
            {
                Title = "You are already registered for this event.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var registration = await registrations.InsertAsync(new RegistrationDocument
        {
            EventId = request.EventId,
            AttendeeId = attendeeId,
            TicketType = string.IsNullOrWhiteSpace(request.TicketType) ? "General" : request.TicketType.Trim(),
            Notes = request.Notes.Trim(),
            Status = RegistrationStatuses.Pending,
            RegisteredAtUtc = DateTime.UtcNow
        }, cancellationToken);

        await InvalidateCaches(registration, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = registration.Id }, registration);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<RegistrationDocument>> Update(
        string id,
        RegistrationUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!RegistrationStatuses.All.Contains(request.Status))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Unknown registration status.");
        }

        var registration = await registrations.FindByIdAsync(id, cancellationToken);
        if (registration is null || !CanAccess(registration))
        {
            return NotFound();
        }

        var isStaff = User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.Organizer);
        registration.Status = isStaff ? request.Status : RegistrationStatuses.Cancelled;
        registration.Notes = request.Notes.Trim();
        await registrations.ReplaceAsync(registration, cancellationToken);
        await InvalidateCaches(registration, cancellationToken);
        return Ok(registration);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var registration = await registrations.FindByIdAsync(id, cancellationToken);
        if (registration is null || !CanAccess(registration))
        {
            return NotFound();
        }

        await registrations.DeleteAsync(id, cancellationToken);
        await InvalidateCaches(registration, cancellationToken);
        return NoContent();
    }

    private bool CanAccess(RegistrationDocument registration)
        => User.IsInRole(RoleNames.Admin)
            || User.IsInRole(RoleNames.Organizer)
            || string.Equals(registration.AttendeeId, User.GetUserId(), StringComparison.Ordinal);

    private async Task InvalidateCaches(RegistrationDocument registration, CancellationToken cancellationToken)
    {
        await cache.RemoveAsync(CacheKeys.RegistrationsForEvent(registration.EventId), cancellationToken);
        await cache.RemoveAsync(CacheKeys.RegistrationsForUser(registration.AttendeeId), cancellationToken);
    }
}
