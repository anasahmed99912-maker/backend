using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SecureMessaging.Api.Dtos.Messages;
using SecureMessaging.Api.Extensions;
using SecureMessaging.Api.Services;

namespace SecureMessaging.Api.Hubs;

[Authorize]
public sealed class ChatHub(IConversationService conversationService) : Hub
{
    public async Task JoinConversation(string conversationId)
    {
        var currentUserId = Context.User?.GetRequiredUserId()
            ?? throw new HubException("The current user is not authenticated.");

        if (!await conversationService.IsParticipantAsync(
                currentUserId,
                conversationId,
                Context.ConnectionAborted))
        {
            throw new HubException("You are not a participant in this conversation.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, conversationId, Context.ConnectionAborted);
    }

    public async Task SendEncryptedMessage(SendEncryptedMessageRequest request)
    {
        var currentUserId = Context.User?.GetRequiredUserId()
            ?? throw new HubException("The current user is not authenticated.");

        EncryptedMessageDto message;

        try
        {
            message = await conversationService.SaveMessageAsync(
                currentUserId,
                request,
                Context.ConnectionAborted);
        }
        catch (InvalidOperationException exception)
        {
            throw new HubException(exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new HubException(exception.Message);
        }

        var participantIds = await conversationService.GetParticipantIdsAsync(
            currentUserId,
            request.ConversationId,
            Context.ConnectionAborted);

        await Clients.Users(participantIds)
            .SendAsync("ReceiveEncryptedMessage", message, Context.ConnectionAborted);
    }

    public async Task UpdateEncryptedMessage(UpdateEncryptedMessageRequest request)
    {
        var currentUserId = Context.User?.GetRequiredUserId()
            ?? throw new HubException("The current user is not authenticated.");

        EncryptedMessageDto message;

        try
        {
            message = await conversationService.UpdateMessageAsync(
                currentUserId,
                request,
                Context.ConnectionAborted);
        }
        catch (InvalidOperationException exception)
        {
            throw new HubException(exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new HubException(exception.Message);
        }

        var participantIds = await conversationService.GetParticipantIdsAsync(
            currentUserId,
            request.ConversationId,
            Context.ConnectionAborted);

        await Clients.Users(participantIds)
            .SendAsync("ReceiveEncryptedMessageUpdated", message, Context.ConnectionAborted);
    }

    public async Task DeleteEncryptedMessage(DeleteEncryptedMessageRequest request)
    {
        var currentUserId = Context.User?.GetRequiredUserId()
            ?? throw new HubException("The current user is not authenticated.");

        try
        {
            await conversationService.DeleteMessageAsync(
                currentUserId,
                request,
                Context.ConnectionAborted);
        }
        catch (InvalidOperationException exception)
        {
            throw new HubException(exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new HubException(exception.Message);
        }

        var participantIds = await conversationService.GetParticipantIdsAsync(
            currentUserId,
            request.ConversationId,
            Context.ConnectionAborted);

        await Clients.Users(participantIds)
            .SendAsync(
                "ReceiveEncryptedMessageDeleted",
                new { request.ConversationId, request.MessageId },
                Context.ConnectionAborted);
    }
}
