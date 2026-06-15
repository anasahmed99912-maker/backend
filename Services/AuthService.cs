using SecureMessaging.Api.Dtos.Auth;
using SecureMessaging.Api.Helpers;
using MongoDB.Bson;
using MongoDB.Driver;
using SecureMessaging.Api.Models;
using SecureMessaging.Api.Repositories;

namespace SecureMessaging.Api.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IGoogleTokenValidator googleTokenValidator) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var userName = NormalizeUserName(request.UserName);

        if (userName.Length < 3)
        {
            throw new InvalidOperationException(
                "Username must contain at least 3 letters, numbers, underscores, or hyphens.");
        }

        var existingUser = await userRepository
            .GetByUserNameAsync(userName, cancellationToken);

        if (existingUser is not null)
        {
            throw new InvalidOperationException("Username is already registered.");
        }

        var user = new User
        {
            Id = ObjectId.GenerateNewId().ToString(),
            UserName = userName,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = passwordHasher.HashPassword(request.Password),
            IdentityPublicKeyJwk = IdentityPublicKeyValidator.ValidateAndNormalize(
                request.IdentityPublicKeyJwk)
        };

        try
        {
            await userRepository.CreateAsync(user, cancellationToken);
        }
        catch (MongoWriteException exception) when (
            exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new InvalidOperationException("Username is already registered.");
        }

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var userName = NormalizeUserName(request.UserName);
        var user = await userRepository
            .GetByUserNameAsync(userName, cancellationToken)
            ?? throw new InvalidOperationException("Invalid username or password.");

        if (string.IsNullOrWhiteSpace(user.PasswordHash) ||
            !passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new InvalidOperationException("Invalid username or password.");
        }

        var identityPublicKeyJwk = IdentityPublicKeyValidator.ValidateAndNormalize(
            request.IdentityPublicKeyJwk);

        if (!string.Equals(
                user.IdentityPublicKeyJwk,
                identityPublicKeyJwk,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "This account is linked to a different browser encryption key. " +
                "Use the browser where the account was created.");
        }

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse> SignInWithGoogleAsync(
        GoogleSignInRequest request,
        CancellationToken cancellationToken)
    {
        var payload = await googleTokenValidator.ValidateAsync(request.IdToken, cancellationToken);
        var identityPublicKeyJwk = IdentityPublicKeyValidator.ValidateAndNormalize(
            request.IdentityPublicKeyJwk);

        var user = await userRepository.GetByGoogleSubjectAsync(payload.Subject, cancellationToken)
            ?? await userRepository.GetByEmailAsync(payload.Email, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Id = ObjectId.GenerateNewId().ToString(),
                UserName = await GenerateUserNameAsync(request.PreferredUserName, payload.Email, cancellationToken),
                DisplayName = payload.DisplayName.Trim(),
                Email = payload.Email.Trim(),
                GoogleSubject = payload.Subject,
                AvatarUrl = payload.AvatarUrl,
                IdentityPublicKeyJwk = identityPublicKeyJwk
            };

            await userRepository.CreateAsync(user, cancellationToken);
            return BuildAuthResponse(user);
        }

        user.GoogleSubject = payload.Subject;
        user.Email = payload.Email.Trim();
        user.DisplayName = payload.DisplayName.Trim();
        user.AvatarUrl = payload.AvatarUrl;

        if (!string.Equals(
                user.IdentityPublicKeyJwk,
                identityPublicKeyJwk,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "This account is linked to a different browser encryption key. " +
                "Use the browser where the account was created.");
        }

        await userRepository.ReplaceAsync(user, cancellationToken);
        return BuildAuthResponse(user);
    }

    public async Task<UserProfileDto> GetCurrentUserAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");

        return ToUserProfile(user);
    }

    private async Task<string> GenerateUserNameAsync(
        string preferredUserName,
        string email,
        CancellationToken cancellationToken)
    {
        var baseName = NormalizeUserName(preferredUserName);

        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = NormalizeUserName(email.Split('@', 2)[0]);
        }

        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "cipheruser";
        }

        var candidate = baseName;
        var suffix = 1;

        while (await userRepository.GetByUserNameAsync(candidate, cancellationToken) is not null)
        {
            candidate = $"{baseName}{suffix++}";
        }

        return candidate;
    }

    private static string NormalizeUserName(string value)
    {
        var normalized = new string(value
            .Trim()
            .ToLowerInvariant()
            .Where(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-')
            .ToArray());

        return normalized.Length > 32
            ? normalized[..32]
            : normalized;
    }

    private AuthResponse BuildAuthResponse(User user)
    {
        var (token, expiresAtUtc) = jwtTokenService.CreateToken(user);

        return new AuthResponse(
            token,
            expiresAtUtc,
            ToUserProfile(user));
    }

    private static UserProfileDto ToUserProfile(User user)
    {
        return new UserProfileDto(
            user.Id ?? string.Empty,
            user.UserName,
            user.DisplayName,
            user.IdentityPublicKeyJwk,
            user.Email,
            user.AvatarUrl);
    }
}
