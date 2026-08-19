using EventForge.Api.Data;
using MongoDB.Bson.Serialization.Attributes;

namespace EventForge.Api.Models;

[MongoCollection("events")]
public sealed class EventDocument : MongoDocument
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("slug")]
    public string Slug { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("venueId")]
    public string VenueId { get; set; } = string.Empty;

    [BsonElement("organizerId")]
    public string OrganizerId { get; set; } = string.Empty;

    [BsonElement("startsAtUtc")]
    public DateTime StartsAtUtc { get; set; }

    [BsonElement("endsAtUtc")]
    public DateTime EndsAtUtc { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = EventStatuses.Draft;

    [BsonElement("tags")]
    public List<string> Tags { get; set; } = [];
}
