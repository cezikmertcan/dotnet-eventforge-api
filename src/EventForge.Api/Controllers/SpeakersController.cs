using EventForge.Api.Data;
using EventForge.Api.Infrastructure;
using EventForge.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EventForge.Api.Controllers;

[ApiController]
[Route("api/speakers")]
[Authorize]
public sealed class SpeakersController(
    IMongoRepository<SpeakerDocument> speakers,
    IMongoRepository<SessionDocument> sessions,
    ICacheService cache) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SpeakerDocument>>> List(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var cached = await cache.GetAsync<List<SpeakerDocument>>(CacheKeys.SpeakersList, cancellationToken);
        if (cached is not null)
        {
            return Ok(cached.Take(Math.Clamp(limit, 1, 100)));
        }

        var result = (await speakers.ListAsync(
            sort: Builders<SpeakerDocument>.Sort.Ascending(item => item.Name),
            cancellationToken: cancellationToken)).ToList();
        await cache.SetAsync(CacheKeys.SpeakersList, result, TimeSpan.FromMinutes(5), cancellationToken);
        return Ok(result.Take(Math.Clamp(limit, 1, 100)));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SpeakerDocument>> Get(string id, CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync<SpeakerDocument>(CacheKeys.Speaker(id), cancellationToken);
        if (cached is not null)
        {
            return Ok(cached);
        }

        var speaker = await speakers.FindByIdAsync(id, cancellationToken);
        if (speaker is null)
        {
            return NotFound();
        }

        await cache.SetAsync(CacheKeys.Speaker(id), speaker, TimeSpan.FromMinutes(5), cancellationToken);
        return Ok(speaker);
    }

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.Organizer)]
    public async Task<ActionResult<SpeakerDocument>> Create(SpeakerRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Speaker name is required.");
        }

        var speaker = await speakers.InsertAsync(ToDocument(request), cancellationToken);
        await cache.RemoveAsync(CacheKeys.SpeakersList, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = speaker.Id }, speaker);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.Organizer)]
    public async Task<ActionResult<SpeakerDocument>> Update(
        string id,
        SpeakerRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Speaker name is required.");
        }

        var speaker = await speakers.FindByIdAsync(id, cancellationToken);
        if (speaker is null)
        {
            return NotFound();
        }

        speaker.Name = request.Name.Trim();
        speaker.Bio = request.Bio.Trim();
        speaker.Company = request.Company.Trim();
        speaker.ProfileUrl = string.IsNullOrWhiteSpace(request.ProfileUrl) ? null : request.ProfileUrl.Trim();
        speaker.Topics = request.Topics.Select(topic => topic.Trim()).Where(topic => topic.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        await speakers.ReplaceAsync(speaker, cancellationToken);
        await cache.RemoveAsync(CacheKeys.SpeakersList, cancellationToken);
        await cache.RemoveAsync(CacheKeys.Speaker(id), cancellationToken);
        await cache.RemoveAsync(CacheKeys.SessionsList, cancellationToken);
        return Ok(speaker);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.Organizer)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        if (await sessions.FindOneAsync(Builders<SessionDocument>.Filter.AnyEq(item => item.SpeakerIds, id), cancellationToken) is not null)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Speaker is referenced by a session.",
                Detail = "Remove the speaker from related sessions before deleting this document.",
                Status = StatusCodes.Status409Conflict
            });
        }

        if (!await speakers.DeleteAsync(id, cancellationToken))
        {
            return NotFound();
        }

        await cache.RemoveAsync(CacheKeys.SpeakersList, cancellationToken);
        await cache.RemoveAsync(CacheKeys.Speaker(id), cancellationToken);
        return NoContent();
    }

    private static SpeakerDocument ToDocument(SpeakerRequest request) => new()
    {
        Name = request.Name.Trim(),
        Bio = request.Bio.Trim(),
        Company = request.Company.Trim(),
        ProfileUrl = string.IsNullOrWhiteSpace(request.ProfileUrl) ? null : request.ProfileUrl.Trim(),
        Topics = request.Topics.Select(topic => topic.Trim()).Where(topic => topic.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
    };
}
