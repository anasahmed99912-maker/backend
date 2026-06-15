namespace SecureMessaging.Api.Configuration;

public sealed class GoogleOAuthOptions
{
    public const string SectionName = "GoogleOAuth";

    public string ClientId { get; init; } = string.Empty;
}
