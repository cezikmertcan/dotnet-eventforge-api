namespace EventForge.Api.Models;

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Organizer = "Organizer";
    public const string Attendee = "Attendee";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Admin,
        Organizer,
        Attendee
    };
}
