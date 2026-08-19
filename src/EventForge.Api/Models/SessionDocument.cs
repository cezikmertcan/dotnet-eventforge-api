using EventForge.Api.Data;
using MongoDB.Bson.Serialization.Attributes;

namespace EventForge.Api.Models;

[MongoCollection("sessions")]
public sealed class SessionDocument : MongoDocument
{
    [BsonElement("eventId")]
    public string EventId { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("abstract")]
    public string Abstract { get; set; } = string.Empty;

    [BsonElement("track")]
    public string Track { get; set; } = string.Empty;

    [BsonElement("room")]
    public string Room { get; set; } = string.Empty;

    [BsonElement("startsAtUtc")]
    public DateTime StartsAtUtc { get; set; }

    [BsonElement("endsAtUtc")]
    public DateTime EndsAtUtc { get; set; }

    [BsonElement("speakerIds")]
    public List<string> SpeakerIds { get; set; } = [];
}
