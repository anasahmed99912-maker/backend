using SecureMessaging.Api.Models;

namespace SecureMessaging.Api.Services;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(User user);
}
