using EventForge.Api.Data;
using MongoDB.Bson.Serialization.Attributes;

namespace EventForge.Api.Models;

[MongoCollection("users")]
public sealed class UserDocument : MongoDocument
{
    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    [BsonElement("role")]
    public string Role { get; set; } = RoleNames.Attendee;

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}
