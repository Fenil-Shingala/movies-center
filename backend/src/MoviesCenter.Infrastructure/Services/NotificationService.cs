using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Common.Models;
using MoviesCenter.Application.Features.Notifications.Dtos;
using MoviesCenter.Domain.Entities;
using MoviesCenter.Infrastructure.Data;

namespace MoviesCenter.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ApplicationDbContext context,
        IEmailService emailService,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<PagedResult<NotificationDto>> GetNotificationsAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto(
                n.Id,
                n.Type,
                n.Title,
                n.Body,
                n.Data,
                n.IsRead,
                n.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return new PagedResult<NotificationDto>(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
    }

    public async Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);

        if (notification == null) return false;

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var unreadList = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync(cancellationToken);

        if (unreadList.Count == 0) return 0;

        var now = DateTime.UtcNow;
        foreach (var n in unreadList)
        {
            n.IsRead = true;
            n.ReadAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return unreadList.Count;
    }

    public async Task<bool> DeleteNotificationAsync(Guid userId, Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);

        if (notification == null) return false;

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<NotificationSettingsDto> GetSettingsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var settings = await _context.NotificationSettings
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        if (settings == null)
        {
            settings = new NotificationSettings
            {
                UserId = userId,
                NewEpisodeInApp = true,
                NewEpisodeEmail = true,
                NewEpisodePush = false,
                ContinueWatchingInApp = true,
                ContinueWatchingEmail = false,
                ContinueWatchingPush = false,
                UpdatedAt = DateTime.UtcNow
            };

            _context.NotificationSettings.Add(settings);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new NotificationSettingsDto(
            settings.NewEpisodeInApp,
            settings.NewEpisodeEmail,
            settings.NewEpisodePush,
            settings.ContinueWatchingInApp,
            settings.ContinueWatchingEmail,
            settings.ContinueWatchingPush
        );
    }

    public async Task<NotificationSettingsDto> UpdateSettingsAsync(
        Guid userId,
        NotificationSettingsDto dto,
        CancellationToken cancellationToken = default)
    {
        var settings = await _context.NotificationSettings
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        if (settings == null)
        {
            settings = new NotificationSettings
            {
                UserId = userId
            };
            _context.NotificationSettings.Add(settings);
        }

        settings.NewEpisodeInApp = dto.NewEpisodeInApp;
        settings.NewEpisodeEmail = dto.NewEpisodeEmail;
        settings.NewEpisodePush = dto.NewEpisodePush;
        settings.ContinueWatchingInApp = dto.ContinueWatchingInApp;
        settings.ContinueWatchingEmail = dto.ContinueWatchingEmail;
        settings.ContinueWatchingPush = dto.ContinueWatchingPush;
        settings.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new NotificationSettingsDto(
            settings.NewEpisodeInApp,
            settings.NewEpisodeEmail,
            settings.NewEpisodePush,
            settings.ContinueWatchingInApp,
            settings.ContinueWatchingEmail,
            settings.ContinueWatchingPush
        );
    }

    public async Task<NotificationDto> CreateNotificationAsync(
        Guid userId,
        string type,
        string title,
        string body,
        string? data = null,
        CancellationToken cancellationToken = default)
    {
        var settings = await _context.NotificationSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        // Check if user disabled in-app for this specific type
        var allowInApp = true;
        var allowEmail = false;

        if (settings != null)
        {
            if (string.Equals(type, "NewEpisode", StringComparison.OrdinalIgnoreCase))
            {
                allowInApp = settings.NewEpisodeInApp;
                allowEmail = settings.NewEpisodeEmail;
            }
            else if (string.Equals(type, "ContinueWatchingReminder", StringComparison.OrdinalIgnoreCase))
            {
                allowInApp = settings.ContinueWatchingInApp;
                allowEmail = settings.ContinueWatchingEmail;
            }
        }

        Notification? entity = null;
        if (allowInApp)
        {
            entity = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = type,
                Title = title,
                Body = body,
                Data = data,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }

        if (allowEmail)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
            if (!string.IsNullOrWhiteSpace(user?.Email))
            {
                var htmlBody = $"<h2>{title}</h2><p>{body}</p><p><a href=\"http://localhost:4200\">Open Movies Center</a></p>";
                await _emailService.SendEmailAsync(user.Email, title, htmlBody, cancellationToken);
            }
        }

        return entity != null
            ? new NotificationDto(entity.Id, entity.Type, entity.Title, entity.Body, entity.Data, entity.IsRead, entity.CreatedAt)
            : new NotificationDto(Guid.NewGuid(), type, title, body, data, false, DateTime.UtcNow);
    }

    public async Task<int> BroadcastSystemNotificationAsync(
        string title,
        string body,
        string? data = null,
        string? type = null,
        CancellationToken cancellationToken = default)
    {
        var notifType = string.IsNullOrWhiteSpace(type) ? "SystemAlert" : type;
        var activeUserIds = await _context.Users
            .Where(u => u.IsActive)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        if (activeUserIds.Count == 0) return 0;

        var now = DateTime.UtcNow;
        var notifications = activeUserIds.Select(uId => new Notification
        {
            Id = Guid.NewGuid(),
            UserId = uId,
            Type = notifType,
            Title = title,
            Body = body,
            Data = data,
            IsRead = false,
            CreatedAt = now
        }).ToList();

        await _context.Notifications.AddRangeAsync(notifications, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Broadcasted system notification '{Title}' to {Count} users", title, notifications.Count);
        return notifications.Count;
    }

    public async Task<int> SendContinueWatchingRemindersAsync(CancellationToken cancellationToken = default)
    {
        // Find unfinished watch history entries last updated more than 2 days ago
        var twoDaysAgo = DateTime.UtcNow.AddDays(-2);

        var unfinished = await _context.WatchHistoryItems
            .Include(w => w.Title)
            .Include(w => w.User)
            .Where(w => !w.IsCompleted && w.ProgressSeconds > 60 && w.WatchedAt <= twoDaysAgo)
            .Take(50)
            .ToListAsync(cancellationToken);

        var dispatched = 0;
        foreach (var item in unfinished)
        {
            if (item.Title == null) continue;

            // Check if reminder was already sent in the past 7 days
            var recentReminderExists = await _context.Notifications
                .AnyAsync(n => n.UserId == item.UserId &&
                               n.Type == "ContinueWatchingReminder" &&
                               n.Data != null && n.Data.Contains(item.TitleId.ToString()) &&
                               n.CreatedAt >= DateTime.UtcNow.AddDays(-7), cancellationToken);

            if (recentReminderExists) continue;

            var dataJson = JsonSerializer.Serialize(new
            {
                titleId = item.TitleId,
                title = item.Title.TitleText,
                type = item.Title.Type,
                episodeId = item.EpisodeId,
                actionUrl = $"/watch/{item.Title.Type}/{item.TitleId}"
            });

            await CreateNotificationAsync(
                item.UserId,
                "ContinueWatchingReminder",
                $"Ready to continue {item.Title.TitleText}?",
                $"Pick up right where you left off at {item.ProgressSeconds / 60}m.",
                dataJson,
                cancellationToken);

            dispatched++;
        }

        return dispatched;
    }
}
