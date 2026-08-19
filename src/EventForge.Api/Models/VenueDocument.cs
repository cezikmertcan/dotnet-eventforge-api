using EventForge.Api.Data;
using MongoDB.Bson.Serialization.Attributes;

namespace EventForge.Api.Models;

[MongoCollection("venues")]
public sealed class VenueDocument : MongoDocument
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("address")]
    public string Address { get; set; } = string.Empty;

    [BsonElement("city")]
    public string City { get; set; } = string.Empty;

    [BsonElement("country")]
    public string Country { get; set; } = string.Empty;

    [BsonElement("capacity")]
    public int Capacity { get; set; }

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}
