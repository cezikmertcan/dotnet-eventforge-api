using EventForge.Api.Authentication;
using EventForge.Api.Data;
using EventForge.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EventForge.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class UsersController(IMongoRepository<UserDocument> users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserProfile>>> List(CancellationToken cancellationToken)
    {
        var documents = await users.ListAsync(
            sort: Builders<UserDocument>.Sort.Ascending(user => user.Email),
            cancellationToken: cancellationToken);

        return Ok(documents.Select(user => user.ToProfile()).ToArray());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserProfile>> Get(string id, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(id, cancellationToken);
        return user is null ? NotFound() : Ok(user.ToProfile());
    }

    [HttpPatch("{id}/role")]
    public async Task<ActionResult<UserProfile>> UpdateRole(
        string id,
        RoleUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (!RoleNames.All.Contains(request.Role))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Unknown role.");
        }

        var user = await users.FindByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        user.Role = request.Role;
        await users.ReplaceAsync(user, cancellationToken);
        return Ok(user.ToProfile());
    }
}
