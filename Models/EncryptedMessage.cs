using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace SecureMessaging.Api.Models;

[BsonIgnoreExtraElements]
public sealed class EncryptedMessage
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("conversationId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public required string ConversationId { get; set; }

    [BsonElement("senderUserId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public required string SenderUserId { get; set; }

    [BsonElement("clientMessageId")]
    public string? ClientMessageId { get; set; }

    [BsonElement("ciphertextBase64")]
    public required string CiphertextBase64 { get; set; }

    [BsonElement("ivBase64")]
    public required string IvBase64 { get; set; }

    [BsonElement("encryptionAlgorithm")]
    public string EncryptionAlgorithm { get; set; } = "AES-256-GCM";

    [BsonElement("attachment")]
    public EncryptedAttachment? Attachment { get; set; }

    [BsonElement("sentAtUtc")]
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? UpdatedAtUtc { get; set; }
}
