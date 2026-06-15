using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using SecureMessaging.Api.Configuration;

namespace SecureMessaging.Api.Services;

public sealed class GoogleTokenValidator(IOptions<GoogleOAuthOptions> options) : IGoogleTokenValidator
{
    public async Task<GoogleTokenPayload> ValidateAsync(
        string idToken,
        CancellationToken cancellationToken)
    {
        var googleOptions = options.Value;

        if (string.IsNullOrWhiteSpace(googleOptions.ClientId) ||
            googleOptions.ClientId.StartsWith("replace-with-your-google-client-id", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Google OAuth ClientId is not configured.");
        }

        GoogleJsonWebSignature.Payload payload;

        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                idToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [googleOptions.ClientId]
                });
        }
        catch (InvalidJwtException)
        {
            throw new InvalidOperationException(
                "Google sign-in token is invalid or expired.");
        }

        if (!payload.EmailVerified || string.IsNullOrWhiteSpace(payload.Email))
        {
            throw new InvalidOperationException(
                "Google account email must be verified.");
        }

        return new GoogleTokenPayload(
            payload.Subject,
            payload.Email,
            payload.Name ?? payload.Email,
            payload.Picture);
    }
}
