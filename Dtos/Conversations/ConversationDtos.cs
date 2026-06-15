using System.ComponentModel.DataAnnotations;
using SecureMessaging.Api.Dtos.Auth;

namespace SecureMessaging.Api.Dtos.Conversations;

public sealed record CreateConversationRequest(
    [Required] string RecipientUserName);

public sealed record ConversationDto(
    string Id,
    string Type,
    IReadOnlyList<string> ParticipantIds,
    IReadOnlyList<UserProfileDto> Participants,
    DateTime CreatedAtUtc,
    DateTime? LastMessageAtUtc);
