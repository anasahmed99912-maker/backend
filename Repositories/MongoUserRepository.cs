using MongoDB.Bson;
using MongoDB.Driver;
using SecureMessaging.Api.Infrastructure;
using SecureMessaging.Api.Models;
using System.Text.RegularExpressions;

namespace SecureMessaging.Api.Repositories;

public sealed class MongoUserRepository(MongoDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(string userId, CancellationToken cancellationToken)
    {
        return await context.Users
            .Find(user => user.Id == userId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken)
    {
        return await context.Users
            .Find(user => user.UserName == userName)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return await context.Users
            .Find(user => user.Email == email)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<User?> GetByGoogleSubjectAsync(
        string googleSubject,
        CancellationToken cancellationToken)
    {
        return await context.Users
            .Find(user => user.GoogleSubject == googleSubject)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetByIdsAsync(
        IEnumerable<string> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct(StringComparer.Ordinal).ToList();

        return await context.Users
            .Find(user => ids.Contains(user.Id!))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> SearchAsync(
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        var trimmedQuery = query.Trim();

        if (string.IsNullOrWhiteSpace(trimmedQuery))
        {
            return [];
        }

        var escapedQuery = Regex.Escape(trimmedQuery);
        var normalizedUserNameQuery = Regex.Escape(trimmedQuery.ToLowerInvariant());
        var filter = Builders<User>.Filter.Or(
            Builders<User>.Filter.Regex(
                user => user.UserName,
                new BsonRegularExpression($"^{normalizedUserNameQuery}", "i")),
            Builders<User>.Filter.Regex(
                user => user.DisplayName,
                new BsonRegularExpression(escapedQuery, "i")),
            Builders<User>.Filter.Regex(
                user => user.Email,
                new BsonRegularExpression(escapedQuery, "i")));

        return await context.Users
            .Find(filter)
            .Limit(Math.Clamp(limit, 1, 10))
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(User user, CancellationToken cancellationToken)
    {
        await context.Users.InsertOneAsync(user, cancellationToken: cancellationToken);
    }

    public async Task ReplaceAsync(User user, CancellationToken cancellationToken)
    {
        await context.Users.ReplaceOneAsync(
            existingUser => existingUser.Id == user.Id,
            user,
            cancellationToken: cancellationToken);
    }
}
