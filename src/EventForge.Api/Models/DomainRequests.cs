namespace EventForge.Api.Models;

public sealed class VenueRequest
{
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public int Capacity { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed class EventRequest
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string VenueId { get; init; } = string.Empty;
    public DateTime StartsAtUtc { get; init; }
    public DateTime EndsAtUtc { get; init; }
    public string Status { get; init; } = EventStatuses.Draft;
    public IReadOnlyList<string> Tags { get; init; } = [];
}

public sealed class SpeakerRequest
{
    public string Name { get; init; } = string.Empty;
    public string Bio { get; init; } = string.Empty;
    public string Company { get; init; } = string.Empty;
    public string? ProfileUrl { get; init; }
    public IReadOnlyList<string> Topics { get; init; } = [];
}

public sealed class SessionRequest
{
    public string EventId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Abstract { get; init; } = string.Empty;
    public string Track { get; init; } = string.Empty;
    public string Room { get; init; } = string.Empty;
    public DateTime StartsAtUtc { get; init; }
    public DateTime EndsAtUtc { get; init; }
    public IReadOnlyList<string> SpeakerIds { get; init; } = [];
}

public sealed class RegistrationRequest
{
    public string EventId { get; init; } = string.Empty;
    public string TicketType { get; init; } = "General";
    public string Notes { get; init; } = string.Empty;
}

public sealed class RegistrationUpdateRequest
{
    public string Status { get; init; } = RegistrationStatuses.Pending;
    public string Notes { get; init; } = string.Empty;
}
