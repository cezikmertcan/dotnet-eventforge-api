using EventForge.Api.Infrastructure;
using EventForge.Api.Models;

namespace EventForge.Api.Tests;

public sealed class DomainContractTests
{
    [Fact]
    public void RolesExposeOnlySupportedValues()
    {
        Assert.Equal(3, RoleNames.All.Count);
        Assert.Contains(RoleNames.Admin, RoleNames.All);
        Assert.Contains(RoleNames.Organizer, RoleNames.All);
        Assert.Contains(RoleNames.Attendee, RoleNames.All);
    }

    [Fact]
    public void CacheKeysKeepResourceBoundariesExplicit()
    {
        Assert.Equal("events:event-123", CacheKeys.Event("event-123"));
        Assert.Equal("registrations:event:event-123", CacheKeys.RegistrationsForEvent("event-123"));
        Assert.Equal("registrations:user:user-123", CacheKeys.RegistrationsForUser("user-123"));
    }
}
