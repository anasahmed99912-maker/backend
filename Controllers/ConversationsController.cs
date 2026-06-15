using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureMessaging.Api.Dtos.Conversations;
using SecureMessaging.Api.Dtos.Messages;
using SecureMessaging.Api.Extensions;
using SecureMessaging.Api.Services;

namespace SecureMessaging.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/conversations")]
public sealed class ConversationsController(IConversationService conversationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ConversationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ConversationDto>>> List(CancellationToken cancellationToken)
    {
        var currentUserId = User.GetRequiredUserId();
        var conversations = await conversationService.ListForUserAsync(currentUserId, cancellationToken);
        return Ok(conversations);
    }

    [HttpPost]
    [ProducesResponseType<ConversationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConversationDto>> Create(
        [FromBody] CreateConversationRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = User.GetRequiredUserId();

        try
        {
            return Ok(await conversationService
                .CreateDirectConversationAsync(currentUserId, request, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpGet("{conversationId}/messages")]
    [ProducesResponseType<IReadOnlyList<EncryptedMessageDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<EncryptedMessageDto>>> Messages(
        string conversationId,
        CancellationToken cancellationToken)
    {
        var currentUserId = User.GetRequiredUserId();

        try
        {
            var messages = await conversationService.GetMessagesAsync(
                currentUserId,
                conversationId,
                cancellationToken);

            return Ok(messages);
        }
        catch (UnauthorizedAccessException exception)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = exception.Message });
        }
    }
}
