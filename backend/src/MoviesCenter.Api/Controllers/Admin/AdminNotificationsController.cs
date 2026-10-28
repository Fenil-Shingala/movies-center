using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Notifications.Dtos;

namespace MoviesCenter.Api.Controllers.Admin;

[ApiController]
[Route("api/v1/admin/notifications")]
[Authorize(Roles = "Admin")]
public class AdminNotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public AdminNotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Broadcast a notification announcement to all active users.
    /// </summary>
    [HttpPost("broadcast")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Broadcast(
        [FromBody] BroadcastNotificationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Body))
        {
            return BadRequest(new { message = "Title and Body are required." });
        }

        var count = await _notificationService.BroadcastSystemNotificationAsync(
            request.Title,
            request.Body,
            request.Data,
            request.Type,
            cancellationToken);

        return Ok(new { success = true, recipientCount = count });
    }

    /// <summary>
    /// Trigger continue-watching reminder notifications for users who left titles unfinished.
    /// </summary>
    [HttpPost("trigger-continue-reminders")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TriggerContinueReminders(CancellationToken cancellationToken)
    {
        var sent = await _notificationService.SendContinueWatchingRemindersAsync(cancellationToken);
        return Ok(new { success = true, remindersDispatched = sent });
    }
}
