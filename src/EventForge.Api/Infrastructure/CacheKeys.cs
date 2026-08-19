namespace EventForge.Api.Infrastructure;

public static class CacheKeys
{
    public const string VenuesList = "venues:list";
    public const string EventsList = "events:list";
    public const string SpeakersList = "speakers:list";
    public const string SessionsList = "sessions:list";

    public static string Venue(string id) => $"venues:{id}";
    public static string Event(string id) => $"events:{id}";
    public static string Speaker(string id) => $"speakers:{id}";
    public static string Session(string id) => $"sessions:{id}";
    public static string RegistrationsForEvent(string eventId) => $"registrations:event:{eventId}";
    public static string RegistrationsForUser(string userId) => $"registrations:user:{userId}";
}
