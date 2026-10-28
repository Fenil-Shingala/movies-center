using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.Notifications.Dtos;

namespace MoviesCenter.Api.Controllers;

[ApiController]
[Route("api/v1/me")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IPushNotificationService _pushNotificationService;

    public NotificationsController(
        INotificationService notificationService,
        IPushNotificationService pushNotificationService)
    {
        _notificationService = notificationService;
        _pushNotificationService = pushNotificationService;
    }

    /// <summary>
    /// Get paginated notifications for the current authenticated user.
    /// </summary>
    [HttpGet("notifications")]
    [ProducesResponseType(typeof(PagedResult<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<NotificationDto>>> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var result = await _notificationService.GetNotificationsAsync(userId, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get the unread notification count for the bell badge.
    /// </summary>
    [HttpGet("notifications/unread-count")]
    [ProducesResponseType(typeof(UnreadCountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UnreadCountDto>> GetUnreadCount(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var count = await _notificationService.GetUnreadCountAsync(userId, cancellationToken);
        return Ok(new UnreadCountDto(count));
    }

    /// <summary>
    /// Mark a specific notification as read.
    /// </summary>
    [HttpPost("notifications/{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var success = await _notificationService.MarkAsReadAsync(userId, id, cancellationToken);
        if (!success) return NotFound(new { message = "Notification not found or access denied." });

        return Ok(new { success = true });
    }

    /// <summary>
    /// Mark all notifications as read for current user.
    /// </summary>
    [HttpPost("notifications/read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var updatedCount = await _notificationService.MarkAllAsReadAsync(userId, cancellationToken);
        return Ok(new { success = true, markedCount = updatedCount });
    }

    /// <summary>
    /// Delete a notification from current user's inbox.
    /// </summary>
    [HttpDelete("notifications/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteNotification(Guid id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var success = await _notificationService.DeleteNotificationAsync(userId, id, cancellationToken);
        if (!success) return NotFound(new { message = "Notification not found." });

        return Ok(new { success = true });
    }

    /// <summary>
    /// Get the user's notification preferences and channel toggles.
    /// </summary>
    [HttpGet("notification-settings")]
    [ProducesResponseType(typeof(NotificationSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<NotificationSettingsDto>> GetSettings(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var settings = await _notificationService.GetSettingsAsync(userId, cancellationToken);
        return Ok(settings);
    }

    /// <summary>
    /// Update notification preferences (In-app, Email, Push).
    /// </summary>
    [HttpPut("notification-settings")]
    [ProducesResponseType(typeof(NotificationSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<NotificationSettingsDto>> UpdateSettings(
        [FromBody] NotificationSettingsDto dto,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var updated = await _notificationService.UpdateSettingsAsync(userId, dto, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// Convenience endpoint to send a sample test notification to oneself (useful for dev testing).
    /// </summary>
    [HttpPost("notifications/test-seed")]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<NotificationDto>> SendTestNotification(
        [FromQuery] string type = "NewEpisode",
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var title = type == "ContinueWatchingReminder"
            ? "Ready to finish Inception?"
            : "New Episode: House of the Dragon S02E04 is out!";
        var body = type == "ContinueWatchingReminder"
            ? "You have 45 minutes remaining. Pick up right where you left off."
            : "Stream the newest episode in 4K UHD now.";
        var data = "{\"actionUrl\":\"/\"}";

        var notif = await _notificationService.CreateNotificationAsync(userId, type, title, body, data, cancellationToken);
        return Ok(notif);
    }

    /// <summary>
    /// Get the VAPID Public Key for client-side Web Push subscription.
    /// </summary>
    [HttpGet("push-subscriptions/vapid-public-key")]
    [ProducesResponseType(typeof(VapidPublicKeyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<VapidPublicKeyDto>> GetVapidPublicKey()
    {
        var key = await _pushNotificationService.GetVapidPublicKeyAsync();
        return Ok(new VapidPublicKeyDto(key));
    }

    /// <summary>
    /// Register a browser push subscription for the authenticated user.
    /// </summary>
    [HttpPost("push-subscriptions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegisterPushSubscription(
        [FromBody] PushSubscriptionDto dto,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var success = await _pushNotificationService.RegisterSubscriptionAsync(userId, dto, cancellationToken);
        if (!success) return BadRequest(new { message = "Failed to register push subscription." });

        return Ok(new { success = true });
    }

    /// <summary>
    /// Unregister a browser push subscription for the authenticated user.
    /// </summary>
    [HttpDelete("push-subscriptions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UnregisterPushSubscription(
        [FromBody] UnregisterPushSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var success = await _pushNotificationService.UnregisterSubscriptionAsync(userId, request.Endpoint, cancellationToken);
        return Ok(new { success });
    }

    private bool TryGetUserId(out Guid userId)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) 
            ?? User.FindFirstValue("sub");

        return Guid.TryParse(userIdStr, out userId);
    }
}
