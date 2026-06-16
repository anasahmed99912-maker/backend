using SecureMessaging.Api.Dtos.Auth;
using SecureMessaging.Api.Dtos.Conversations;
using SecureMessaging.Api.Dtos.Messages;
using SecureMessaging.Api.Helpers;
using MongoDB.Bson;
using MongoDB.Driver;
using SecureMessaging.Api.Models;
using SecureMessaging.Api.Repositories;

namespace SecureMessaging.Api.Services;

public sealed class ConversationService(
    IUserRepository userRepository,
    IConversationRepository conversationRepository,
    IMessageRepository messageRepository) : IConversationService
{
    private const int MaxTextCiphertextBytes = 64 * 1024;
    private const int MaxAttachmentBytes = 5 * 1024 * 1024;

    public async Task<ConversationDto> CreateDirectConversationAsync(
        string currentUserId,
        CreateConversationRequest request,
        CancellationToken cancellationToken)
    {
        var recipient = await userRepository
            .GetByUserNameAsync(
                request.RecipientUserName.Trim().ToLowerInvariant(),
                cancellationToken)
            ?? throw new InvalidOperationException("Recipient user was not found.");

        if (recipient.Id == currentUserId)
        {
            throw new InvalidOperationException("You cannot create a conversation with yourself.");
        }

        var conversationKey = ConversationKeyBuilder.BuildDirect([currentUserId, recipient.Id!]);
        var existing = await conversationRepository.GetByKeyAsync(conversationKey, cancellationToken);

        if (existing is not null)
        {
            return await ToConversationDtoAsync(existing, cancellationToken);
        }

        var conversation = new Conversation
        {
            Id = ObjectId.GenerateNewId().ToString(),
            Type = "direct",
            ConversationKey = conversationKey,
            ParticipantIds = [currentUserId, recipient.Id!]
        };

        try
        {
            await conversationRepository.CreateAsync(conversation, cancellationToken);
        }
        catch (MongoWriteException exception) when (
            exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            var concurrentlyCreated = await conversationRepository.GetByKeyAsync(
                conversationKey,
                cancellationToken);

            if (concurrentlyCreated is not null)
            {
                return await ToConversationDtoAsync(
                    concurrentlyCreated,
                    cancellationToken);
            }

            throw;
        }

        return await ToConversationDtoAsync(conversation, cancellationToken);
    }

    public async Task<IReadOnlyList<ConversationDto>> ListForUserAsync(
        string currentUserId,
        CancellationToken cancellationToken)
    {
        var conversations = await conversationRepository.ListForUserAsync(currentUserId, cancellationToken);
        var items = new List<ConversationDto>(conversations.Count);

        foreach (var conversation in conversations)
        {
            items.Add(await ToConversationDtoAsync(conversation, cancellationToken));
        }

        return items;
    }

    public async Task<IReadOnlyList<EncryptedMessageDto>> GetMessagesAsync(
        string currentUserId,
        string conversationId,
        CancellationToken cancellationToken)
    {
        await EnsureParticipantAsync(currentUserId, conversationId, cancellationToken);
        var messages = await messageRepository.ListByConversationAsync(conversationId, cancellationToken);
        return messages.Select(ToMessageDto).ToList();
    }

    public async Task<EncryptedMessageDto> SaveMessageAsync(
        string currentUserId,
        SendEncryptedMessageRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureParticipantAsync(currentUserId, request.ConversationId, cancellationToken);
        ValidateMessage(request);

        if (!string.IsNullOrWhiteSpace(request.ClientMessageId))
        {
            var existing = await messageRepository.GetByClientMessageIdAsync(
                request.ConversationId,
                currentUserId,
                request.ClientMessageId,
                cancellationToken);

            if (existing is not null)
            {
                return ToMessageDto(existing);
            }
        }

        var message = new EncryptedMessage
        {
            Id = ObjectId.GenerateNewId().ToString(),
            ConversationId = request.ConversationId,
            SenderUserId = currentUserId,
            ClientMessageId = request.ClientMessageId,
            CiphertextBase64 = request.CiphertextBase64,
            IvBase64 = request.IvBase64,
            EncryptionAlgorithm = string.IsNullOrWhiteSpace(request.EncryptionAlgorithm)
                ? "AES-256-GCM"
                : request.EncryptionAlgorithm.Trim(),
            Attachment = request.Attachment is null
                ? null
                : new EncryptedAttachment
                {
                    FileName = request.Attachment.FileName,
                    MimeType = request.Attachment.MimeType,
                    CiphertextBase64 = request.Attachment.CiphertextBase64,
                    IvBase64 = request.Attachment.IvBase64,
                    SizeBytes = request.Attachment.SizeBytes
                }
        };

        try
        {
            await messageRepository.CreateAsync(message, cancellationToken);
        }
        catch (MongoWriteException exception) when (
            exception.WriteError?.Category == ServerErrorCategory.DuplicateKey &&
            !string.IsNullOrWhiteSpace(request.ClientMessageId))
        {
            var existing = await messageRepository.GetByClientMessageIdAsync(
                request.ConversationId,
                currentUserId,
                request.ClientMessageId,
                cancellationToken);

            if (existing is not null)
            {
                return ToMessageDto(existing);
            }

            throw;
        }
        await conversationRepository.UpdateLastMessageAtAsync(
            request.ConversationId,
            message.SentAtUtc,
            cancellationToken);

        return ToMessageDto(message);
    }

    public async Task<UserProfileDto> GetUserByUserNameAsync(
        string userName,
        CancellationToken cancellationToken)
    {
        var user = await userRepository
            .GetByUserNameAsync(userName.Trim().ToLowerInvariant(), cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");

        return ToUserProfile(user);
    }

    public async Task<IReadOnlyList<UserProfileDto>> SearchUsersAsync(
        string query,
        CancellationToken cancellationToken)
    {
        var trimmedQuery = query.Trim();

        if (trimmedQuery.Length < 2)
        {
            return [];
        }

        var normalizedQuery = trimmedQuery.ToLowerInvariant();
        var users = await userRepository.SearchAsync(trimmedQuery, 8, cancellationToken);

        return users
            .OrderBy(user => GetSearchRank(user, normalizedQuery))
            .ThenBy(user => user.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(user => user.UserName, StringComparer.OrdinalIgnoreCase)
            .Select(ToUserProfile)
            .ToList();
    }

    private static void ValidateMessage(SendEncryptedMessageRequest request)
    {
        if (!string.Equals(
                request.EncryptionAlgorithm,
                "AES-256-GCM",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Only AES-256-GCM messages are accepted.");
        }

        ValidateBase64(
            request.CiphertextBase64,
            nameof(request.CiphertextBase64),
            MaxTextCiphertextBytes);
        ValidateIv(request.IvBase64);

        if (!string.IsNullOrWhiteSpace(request.ClientMessageId) &&
            !Guid.TryParse(request.ClientMessageId, out _))
        {
            throw new InvalidOperationException("ClientMessageId must be a valid UUID.");
        }

        if (request.Attachment is null)
        {
            return;
        }

        if (request.Attachment.SizeBytes is <= 0 or > MaxAttachmentBytes)
        {
            throw new InvalidOperationException("Attachments must be between 1 byte and 5 MB.");
        }

        if (string.IsNullOrWhiteSpace(request.Attachment.FileName) ||
            request.Attachment.FileName.Length > 255 ||
            string.IsNullOrWhiteSpace(request.Attachment.MimeType) ||
            request.Attachment.MimeType.Length > 128)
        {
            throw new InvalidOperationException("Attachment metadata is invalid.");
        }

        ValidateBase64(
            request.Attachment.CiphertextBase64,
            nameof(request.Attachment.CiphertextBase64),
            MaxAttachmentBytes + 16);
        ValidateIv(request.Attachment.IvBase64);
    }

    private static void ValidateIv(string value)
    {
        var bytes = DecodeBase64(value, "IV");

        if (bytes.Length != 12)
        {
            throw new InvalidOperationException("AES-GCM IV must be exactly 12 bytes.");
        }
    }

    private static void ValidateBase64(string value, string fieldName, int maxBytes)
    {
        var bytes = DecodeBase64(value, fieldName);

        if (bytes.Length < 16 || bytes.Length > maxBytes)
        {
            throw new InvalidOperationException(
                $"{fieldName} has an invalid encrypted payload size.");
        }
    }

    private static byte[] DecodeBase64(string value, string fieldName)
    {
        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException($"{fieldName} must be valid Base64.");
        }
    }

    public async Task<bool> IsParticipantAsync(
        string currentUserId,
        string conversationId,
        CancellationToken cancellationToken)
    {
        var conversation = await conversationRepository.GetByIdAsync(conversationId, cancellationToken);
        return conversation is not null
            && conversation.ParticipantIds.Contains(currentUserId, StringComparer.Ordinal);
    }

    public async Task<IReadOnlyList<string>> GetParticipantIdsAsync(
        string currentUserId,
        string conversationId,
        CancellationToken cancellationToken)
    {
        var conversation = await conversationRepository.GetByIdAsync(
            conversationId,
            cancellationToken);

        if (conversation is null ||
            !conversation.ParticipantIds.Contains(currentUserId, StringComparer.Ordinal))
        {
            throw new UnauthorizedAccessException(
                "You are not a participant in this conversation.");
        }

        return conversation.ParticipantIds;
    }

    private async Task EnsureParticipantAsync(
        string currentUserId,
        string conversationId,
        CancellationToken cancellationToken)
    {
        if (!await IsParticipantAsync(currentUserId, conversationId, cancellationToken))
        {
            throw new UnauthorizedAccessException("You are not a participant in this conversation.");
        }
    }

    private async Task<ConversationDto> ToConversationDtoAsync(
        Conversation conversation,
        CancellationToken cancellationToken)
    {
        var users = await userRepository.GetByIdsAsync(conversation.ParticipantIds, cancellationToken);

        return new ConversationDto(
            conversation.Id ?? string.Empty,
            conversation.Type,
            conversation.ParticipantIds,
            users.Select(ToUserProfile).ToList(),
            conversation.CreatedAtUtc,
            conversation.LastMessageAtUtc);
    }

    private static EncryptedMessageDto ToMessageDto(EncryptedMessage message)
    {
        return new EncryptedMessageDto(
            message.Id ?? string.Empty,
            message.ConversationId,
            message.SenderUserId,
            message.CiphertextBase64,
            message.IvBase64,
            message.EncryptionAlgorithm,
            message.ClientMessageId,
            message.Attachment is null
                ? null
                : new EncryptedAttachmentDto(
                    message.Attachment.FileName,
                    message.Attachment.MimeType,
                    message.Attachment.CiphertextBase64,
                    message.Attachment.IvBase64,
                    message.Attachment.SizeBytes),
            message.SentAtUtc);
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

    private static int GetSearchRank(User user, string query)
    {
        if (string.Equals(user.UserName, query, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (string.Equals(user.Email, query, StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (string.Equals(user.DisplayName, query, StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (user.UserName.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        if (!string.IsNullOrWhiteSpace(user.Email) &&
            user.Email.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 4;
        }

        if (user.DisplayName.StartsWith(query, StringComparison.OrdinalIgnoreCase))
        {
            return 5;
        }

        if (!string.IsNullOrWhiteSpace(user.Email) &&
            user.Email.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return 6;
        }

        if (user.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            return 7;
        }

        return 8;
    }
}
