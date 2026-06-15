using SecureMessaging.Api.Dtos.Auth;

namespace SecureMessaging.Api.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> SignInWithGoogleAsync(GoogleSignInRequest request, CancellationToken cancellationToken);
    Task<UserProfileDto> GetCurrentUserAsync(string userId, CancellationToken cancellationToken);
}
