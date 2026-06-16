using System.ComponentModel.DataAnnotations;

namespace SecureMessaging.Api.Dtos.Messages;

public sealed record EncryptedAttachmentDto(
    [Required] string FileName,
    [Required] string MimeType,
    [Required] string CiphertextBase64,
    [Required] string IvBase64,
    long SizeBytes);

public sealed record SendEncryptedMessageRequest(
    [Required] string ConversationId,
    [Required] string CiphertextBase64,
    [Required] string IvBase64,
    string EncryptionAlgorithm,
    string? ClientMessageId,
    EncryptedAttachmentDto? Attachment);

public sealed record UpdateEncryptedMessageRequest(
    [Required] string MessageId,
    [Required] string ConversationId,
    [Required] string CiphertextBase64,
    [Required] string IvBase64,
    string EncryptionAlgorithm,
    bool PreserveAttachment,
    EncryptedAttachmentDto? Attachment);

public sealed record DeleteEncryptedMessageRequest(
    [Required] string MessageId,
    [Required] string ConversationId);

public sealed record EncryptedMessageDto(
    string Id,
    string ConversationId,
    string SenderUserId,
    string CiphertextBase64,
    string IvBase64,
    string EncryptionAlgorithm,
    string? ClientMessageId,
    EncryptedAttachmentDto? Attachment,
    DateTime SentAtUtc,
    DateTime? UpdatedAtUtc);
