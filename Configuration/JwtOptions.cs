namespace SecureMessaging.Api.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "SecureMessaging.Api";
    public string Audience { get; init; } = "SecureMessaging.Client";
    public string SecretKey { get; init; } = string.Empty;
    public int ExpiresMinutes { get; init; } = 60;
}
