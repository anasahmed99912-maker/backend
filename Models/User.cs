using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SecureMessaging.Api.Models;

[BsonIgnoreExtraElements]
public sealed class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("userName")]
    public required string UserName { get; set; }

    [BsonElement("displayName")]
    public required string DisplayName { get; set; }

    [BsonElement("passwordHash")]
    [BsonIgnoreIfNull]
    public string? PasswordHash { get; set; }

    [BsonElement("email")]
    [BsonIgnoreIfNull]
    public string? Email { get; set; }

    [BsonElement("googleSubject")]
    [BsonIgnoreIfNull]
    public string? GoogleSubject { get; set; }

    [BsonElement("avatarUrl")]
    [BsonIgnoreIfNull]
    public string? AvatarUrl { get; set; }

    [BsonElement("identityPublicKeyJwk")]
    public required string IdentityPublicKeyJwk { get; set; }

    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
