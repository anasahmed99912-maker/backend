using MongoDB.Driver;
using SecureMessaging.Api.Infrastructure;
using SecureMessaging.Api.Models;

namespace SecureMessaging.Api.Repositories;

public sealed class MongoConversationRepository(MongoDbContext context) : IConversationRepository
{
    public async Task<Conversation?> GetByIdAsync(string conversationId, CancellationToken cancellationToken)
    {
        return await context.Conversations
            .Find(conversation => conversation.Id == conversationId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Conversation?> GetByKeyAsync(string conversationKey, CancellationToken cancellationToken)
    {
        return await context.Conversations
            .Find(conversation => conversation.ConversationKey == conversationKey)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Conversation>> ListForUserAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var sort = Builders<Conversation>.Sort
            .Descending(conversation => conversation.LastMessageAtUtc)
            .Descending(conversation => conversation.CreatedAtUtc);

        return await context.Conversations
            .Find(conversation => conversation.ParticipantIds.Contains(userId))
            .Sort(sort)
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(Conversation conversation, CancellationToken cancellationToken)
    {
        await context.Conversations.InsertOneAsync(conversation, cancellationToken: cancellationToken);
    }

    public async Task UpdateLastMessageAtAsync(
        string conversationId,
        DateTime timestamp,
        CancellationToken cancellationToken)
    {
        var update = Builders<Conversation>.Update.Set(
            conversation => conversation.LastMessageAtUtc,
            timestamp);

        await context.Conversations.UpdateOneAsync(
            conversation => conversation.Id == conversationId,
            update,
            cancellationToken: cancellationToken);
    }
}
