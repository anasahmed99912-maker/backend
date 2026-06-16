using SecureMessaging.Api.Dtos.Auth;
using SecureMessaging.Api.Dtos.Conversations;
using SecureMessaging.Api.Dtos.Messages;

namespace SecureMessaging.Api.Services;

public interface IConversationService
{
    Task<ConversationDto> CreateDirectConversationAsync(
        string currentUserId,
        CreateConversationRequest request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ConversationDto>> ListForUserAsync(
        string currentUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EncryptedMessageDto>> GetMessagesAsync(
        string currentUserId,
        string conversationId,
        CancellationToken cancellationToken);

    Task<EncryptedMessageDto> SaveMessageAsync(
        string currentUserId,
        SendEncryptedMessageRequest request,
        CancellationToken cancellationToken);

    Task<UserProfileDto> GetUserByUserNameAsync(
        string userName,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<UserProfileDto>> SearchUsersAsync(
        string query,
        CancellationToken cancellationToken);

    Task<bool> IsParticipantAsync(
        string currentUserId,
        string conversationId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetParticipantIdsAsync(
        string currentUserId,
        string conversationId,
        CancellationToken cancellationToken);
}
