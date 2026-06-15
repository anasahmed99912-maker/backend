namespace SecureMessaging.Api.Services;

public sealed record GoogleTokenPayload(
    string Subject,
    string Email,
    string DisplayName,
    string? AvatarUrl);
