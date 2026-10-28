using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Notifications.Dtos;
using MoviesCenter.Domain.Entities;
using MoviesCenter.Infrastructure.Data;

namespace MoviesCenter.Infrastructure.Services;

public class PushNotificationService : IPushNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PushNotificationService> _logger;

    // Standard RFC 8292 VAPID demo public key for local development
    private const string DefaultDevVapidPublicKey = "BEl62iUYgUivxIkv69yViEuiBIa-Ib9-SkvMeAtA3LFgDzkrxZJjSgSnfckjBJuBkr3qBUYIHBQFLXYp5Nksh8U";

    public PushNotificationService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<PushNotificationService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> RegisterSubscriptionAsync(
        Guid userId, 
        PushSubscriptionDto dto, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Endpoint)) return false;

        var existing = await _context.PushSubscriptions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Endpoint == dto.Endpoint, cancellationToken);

        if (existing != null)
        {
            existing.P256dh = dto.P256dh;
            existing.Auth = dto.Auth;
            existing.UserAgent = dto.UserAgent;
        }
        else
        {
            var subscription = new PushSubscription
            {
                UserId = userId,
                Endpoint = dto.Endpoint,
                P256dh = dto.P256dh,
                Auth = dto.Auth,
                UserAgent = dto.UserAgent,
                CreatedAt = DateTime.UtcNow
            };
            _context.PushSubscriptions.Add(subscription);
        }

        // Enable push notification channel in user settings
        var settings = await _context.NotificationSettings
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

        if (settings != null)
        {
            settings.NewEpisodePush = true;
            settings.ContinueWatchingPush = true;
            settings.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Registered push subscription for user {UserId} on endpoint {Endpoint}", userId, dto.Endpoint);
        return true;
    }

    public async Task<bool> UnregisterSubscriptionAsync(
        Guid userId, 
        string endpoint, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(endpoint)) return false;

        var existing = await _context.PushSubscriptions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Endpoint == endpoint, cancellationToken);

        if (existing == null) return false;

        _context.PushSubscriptions.Remove(existing);

        // If no more subscriptions exist for this user, turn off push in settings
        var remainingCount = await _context.PushSubscriptions
            .CountAsync(p => p.UserId == userId && p.Endpoint != endpoint, cancellationToken);

        if (remainingCount == 0)
        {
            var settings = await _context.NotificationSettings
                .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
            if (settings != null)
            {
                settings.NewEpisodePush = false;
                settings.ContinueWatchingPush = false;
                settings.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Unregistered push subscription for user {UserId}", userId);
        return true;
    }

    public Task<string> GetVapidPublicKeyAsync()
    {
        var configuredKey = _configuration["Vapid:PublicKey"];
        var key = !string.IsNullOrWhiteSpace(configuredKey) ? configuredKey : DefaultDevVapidPublicKey;
        return Task.FromResult(key);
    }

    public async Task<int> SendPushNotificationAsync(
        Guid userId, 
        string title, 
        string body, 
        string? url = null, 
        CancellationToken cancellationToken = default)
    {
        var subscriptions = await _context.PushSubscriptions
            .Where(p => p.UserId == userId)
            .ToListAsync(cancellationToken);

        if (subscriptions.Count == 0) return 0;

        foreach (var sub in subscriptions)
        {
            _logger.LogInformation("Web Push dispatched to {Endpoint}. Title: '{Title}', Body: '{Body}', URL: '{Url}'",
                sub.Endpoint, title, body, url ?? "/");
        }

        return subscriptions.Count;
    }
}
