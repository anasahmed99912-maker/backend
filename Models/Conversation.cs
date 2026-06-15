using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SecureMessaging.Api.Models;

[BsonIgnoreExtraElements]
public sealed class Conversation
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("type")]
    public string Type { get; set; } = "direct";

    [BsonElement("conversationKey")]
    public required string ConversationKey { get; set; }

    [BsonElement("participantIds")]
    public required List<string> ParticipantIds { get; set; } = [];

    [BsonElement("createdAtUtc")]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [BsonElement("lastMessageAtUtc")]
    public DateTime? LastMessageAtUtc { get; set; }
}
