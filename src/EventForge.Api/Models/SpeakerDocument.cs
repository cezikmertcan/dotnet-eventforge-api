using EventForge.Api.Data;
using MongoDB.Bson.Serialization.Attributes;

namespace EventForge.Api.Models;

[MongoCollection("speakers")]
public sealed class SpeakerDocument : MongoDocument
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("bio")]
    public string Bio { get; set; } = string.Empty;

    [BsonElement("company")]
    public string Company { get; set; } = string.Empty;

    [BsonElement("profileUrl")]
    public string? ProfileUrl { get; set; }

    [BsonElement("topics")]
    public List<string> Topics { get; set; } = [];
}
