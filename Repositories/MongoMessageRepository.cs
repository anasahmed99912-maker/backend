using MongoDB.Driver;
using SecureMessaging.Api.Infrastructure;
using SecureMessaging.Api.Models;

namespace SecureMessaging.Api.Repositories;

public sealed class MongoMessageRepository(MongoDbContext context) : IMessageRepository
{
    public async Task CreateAsync(EncryptedMessage message, CancellationToken cancellationToken)
    {
        await context.Messages.InsertOneAsync(message, cancellationToken: cancellationToken);
    }

    public async Task<EncryptedMessage?> GetByIdAsync(
        string messageId,
        CancellationToken cancellationToken)
    {
        return await context.Messages
            .Find(message => message.Id == messageId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<EncryptedMessage?> GetByClientMessageIdAsync(
        string conversationId,
        string senderUserId,
        string clientMessageId,
        CancellationToken cancellationToken)
    {
        return await context.Messages
            .Find(message =>
                message.ConversationId == conversationId &&
                message.SenderUserId == senderUserId &&
                message.ClientMessageId == clientMessageId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EncryptedMessage>> ListByConversationAsync(
        string conversationId,
        CancellationToken cancellationToken)
    {
        return await context.Messages
            .Find(message => message.ConversationId == conversationId)
            .SortBy(message => message.SentAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceAsync(
        EncryptedMessage message,
        CancellationToken cancellationToken)
    {
        await context.Messages.ReplaceOneAsync(
            existing => existing.Id == message.Id,
            message,
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(
        string messageId,
        CancellationToken cancellationToken)
    {
        await context.Messages.DeleteOneAsync(
            message => message.Id == messageId,
            cancellationToken);
    }
}
