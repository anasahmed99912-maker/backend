using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecureMessaging.Api.Dtos.Auth;
using SecureMessaging.Api.Services;

namespace SecureMessaging.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(IConversationService conversationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserProfileDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserProfileDto>>> Search(
        [FromQuery] string query,
        CancellationToken cancellationToken)
    {
        return Ok(await conversationService.SearchUsersAsync(query, cancellationToken));
    }

    [HttpGet("{userName}")]
    [ProducesResponseType<UserProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileDto>> GetByUserName(
        string userName,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await conversationService.GetUserByUserNameAsync(userName, cancellationToken));
        }
        catch (InvalidOperationException exception)
        {
            return NotFound(new { error = exception.Message });
        }
    }
}
