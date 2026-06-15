namespace SecureMessaging.Api.Models;

public sealed class EncryptedAttachment
{
    public required string FileName { get; set; }
    public required string MimeType { get; set; }
    public required string CiphertextBase64 { get; set; }
    public required string IvBase64 { get; set; }
    public long SizeBytes { get; set; }
}
