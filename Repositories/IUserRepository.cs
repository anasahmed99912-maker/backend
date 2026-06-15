using SecureMessaging.Api.Models;

namespace SecureMessaging.Api.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(string userId, CancellationToken cancellationToken);
    Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User?> GetByGoogleSubjectAsync(string googleSubject, CancellationToken cancellationToken);
    Task<IReadOnlyList<User>> GetByIdsAsync(IEnumerable<string> userIds, CancellationToken cancellationToken);
    Task CreateAsync(User user, CancellationToken cancellationToken);
    Task ReplaceAsync(User user, CancellationToken cancellationToken);
}
