using SecureMessaging.Api.Models;

namespace SecureMessaging.Api.Repositories;

public interface IMessageRepository
{
    Task CreateAsync(EncryptedMessage message, CancellationToken cancellationToken);
    Task<EncryptedMessage?> GetByClientMessageIdAsync(
        string conversationId,
        string senderUserId,
        string clientMessageId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<EncryptedMessage>> ListByConversationAsync(string conversationId, CancellationToken cancellationToken);
}
