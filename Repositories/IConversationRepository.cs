using SecureMessaging.Api.Models;

namespace SecureMessaging.Api.Repositories;

public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(string conversationId, CancellationToken cancellationToken);
    Task<Conversation?> GetByKeyAsync(string conversationKey, CancellationToken cancellationToken);
    Task<IReadOnlyList<Conversation>> ListForUserAsync(string userId, CancellationToken cancellationToken);
    Task CreateAsync(Conversation conversation, CancellationToken cancellationToken);
    Task UpdateLastMessageAtAsync(string conversationId, DateTime timestamp, CancellationToken cancellationToken);
}
