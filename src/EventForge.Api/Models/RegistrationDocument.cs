using EventForge.Api.Data;
using MongoDB.Bson.Serialization.Attributes;

namespace EventForge.Api.Models;

[MongoCollection("registrations")]
public sealed class RegistrationDocument : MongoDocument
{
    [BsonElement("eventId")]
    public string EventId { get; set; } = string.Empty;

    [BsonElement("attendeeId")]
    public string AttendeeId { get; set; } = string.Empty;

    [BsonElement("ticketType")]
    public string TicketType { get; set; } = "General";

    [BsonElement("status")]
    public string Status { get; set; } = RegistrationStatuses.Pending;

    [BsonElement("notes")]
    public string Notes { get; set; } = string.Empty;

    [BsonElement("registeredAtUtc")]
    public DateTime RegisteredAtUtc { get; set; } = DateTime.UtcNow;
}
