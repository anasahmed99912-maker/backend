using System.ComponentModel.DataAnnotations;

namespace SecureMessaging.Api.Dtos.Auth;

public sealed record RegisterRequest(
    [Required, StringLength(32, MinimumLength = 3)] string UserName,
    [Required, StringLength(64, MinimumLength = 2)] string DisplayName,
    [Required, StringLength(128, MinimumLength = 8)] string Password,
    [Required, StringLength(1024)] string IdentityPublicKeyJwk);

public sealed record LoginRequest(
    [Required] string UserName,
    [Required] string Password,
    [Required, StringLength(1024)] string IdentityPublicKeyJwk);

public sealed record GoogleSignInRequest(
    [Required] string IdToken,
    [Required, StringLength(32, MinimumLength = 3)] string PreferredUserName,
    [Required, StringLength(1024)] string IdentityPublicKeyJwk);

public sealed record UserProfileDto(
    string Id,
    string UserName,
    string DisplayName,
    string IdentityPublicKeyJwk,
    string? Email,
    string? AvatarUrl);

public sealed record AuthResponse(
    string Token,
    DateTime ExpiresAtUtc,
    UserProfileDto User);
