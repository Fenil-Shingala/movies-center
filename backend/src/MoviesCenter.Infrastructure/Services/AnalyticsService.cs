using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoviesCenter.Application.Common.Interfaces;
using MoviesCenter.Application.Features.Analytics.Dtos;
using MoviesCenter.Domain.Entities;
using MoviesCenter.Infrastructure.Data;

namespace MoviesCenter.Infrastructure.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AnalyticsService> _logger;

    private static readonly HashSet<string> AllowedMetrics = new(StringComparer.OrdinalIgnoreCase)
    {
        "plays",
        "searches",
        "empty_searches",
        "signups",
        "source_fails"
    };

    public AnalyticsService(ApplicationDbContext db, ILogger<AnalyticsService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task IngestEventsAsync(IEnumerable<AnalyticsEventDto> events, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var titleIdsToIncrement = new Dictionary<int, int>();

        // Pre-fetch today's existing stats records for updating
        var existingTodayStats = await _db.AnalyticsDailyStats
            .Where(s => s.Date == today)
            .ToListAsync(cancellationToken);

        foreach (var evt in events)
        {
            if (string.IsNullOrWhiteSpace(evt.EventName))
                continue;

            var normalizedMetric = evt.EventName.Trim().ToLowerInvariant();
            if (!AllowedMetrics.Contains(normalizedMetric))
                continue;

            var dimension = string.IsNullOrWhiteSpace(evt.Dimension) 
                ? null 
                : evt.Dimension.Trim().Length > 500 
                    ? evt.Dimension.Trim().Substring(0, 500) 
                    : evt.Dimension.Trim();

            // Match record in memory
            var stat = existingTodayStats.FirstOrDefault(s => 
                s.MetricName.Equals(normalizedMetric, StringComparison.OrdinalIgnoreCase) && 
                s.Dimension == dimension);

            if (stat == null)
            {
                stat = new AnalyticsDailyStats
                {
                    Date = today,
                    MetricName = normalizedMetric,
                    MetricValue = 1,
                    Dimension = dimension,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.AnalyticsDailyStats.Add(stat);
                existingTodayStats.Add(stat);
            }
            else
            {
                stat.MetricValue += 1;
                stat.UpdatedAt = DateTime.UtcNow;
            }

            // Track title views if metric is "plays" and dimension is titleId
            if (normalizedMetric == "plays" && !string.IsNullOrEmpty(dimension) && int.TryParse(dimension, out var tid))
            {
                titleIdsToIncrement[tid] = titleIdsToIncrement.GetValueOrDefault(tid, 0) + 1;
            }
        }

        // Apply view counts to titles if any
        if (titleIdsToIncrement.Count > 0)
        {
            var tids = titleIdsToIncrement.Keys.ToList();
            var titles = await _db.Titles.Where(t => tids.Contains(t.Id)).ToListAsync(cancellationToken);
            foreach (var t in titles)
            {
                t.ViewCount += titleIdsToIncrement[t.Id];
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AnalyticsOverviewDto> GetAnalyticsOverviewAsync(
        DateOnly? startDate = null, 
        DateOnly? endDate = null, 
        CancellationToken cancellationToken = default)
    {
        var start = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-14));
        var end = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var stats = await _db.AnalyticsDailyStats
            .AsNoTracking()
            .Where(s => s.Date >= start && s.Date <= end)
            .ToListAsync(cancellationToken);

        var totalPlays = stats.Where(s => s.MetricName == "plays").Sum(s => s.MetricValue);
        var totalSearches = stats.Where(s => s.MetricName == "searches").Sum(s => s.MetricValue);
        var totalEmptySearches = stats.Where(s => s.MetricName == "empty_searches").Sum(s => s.MetricValue);
        var totalSignups = stats.Where(s => s.MetricName == "signups").Sum(s => s.MetricValue);

        // Build continuous date range for trends
        var dailyTrends = new List<DailyMetricTrendDto>();
        for (var d = start; d <= end; d = d.AddDays(1))
        {
            var dayStats = stats.Where(s => s.Date == d).ToList();
            dailyTrends.Add(new DailyMetricTrendDto(
                Date: d,
                Plays: dayStats.Where(s => s.MetricName == "plays").Sum(s => s.MetricValue),
                Searches: dayStats.Where(s => s.MetricName == "searches").Sum(s => s.MetricValue),
                Signups: dayStats.Where(s => s.MetricName == "signups").Sum(s => s.MetricValue)
            ));
        }

        // Top played titles
        var playDimensions = stats
            .Where(s => s.MetricName == "plays" && !string.IsNullOrEmpty(s.Dimension))
            .GroupBy(s => s.Dimension!)
            .Select(g => new { Dimension = g.Key, Value = g.Sum(x => x.MetricValue) })
            .OrderByDescending(x => x.Value)
            .Take(10)
            .ToList();

        var topPlayedTitles = new List<TopMetricItemDto>();
        var parsedIds = playDimensions
            .Select(p => int.TryParse(p.Dimension, out var id) ? (int?)id : null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        var titleLookup = await _db.Titles
            .AsNoTracking()
            .Where(t => parsedIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.TitleText, cancellationToken);

        foreach (var p in playDimensions)
        {
            if (int.TryParse(p.Dimension, out var tid) && titleLookup.TryGetValue(tid, out var titleName))
            {
                topPlayedTitles.Add(new TopMetricItemDto(titleName, p.Value, tid));
            }
            else
            {
                topPlayedTitles.Add(new TopMetricItemDto(p.Dimension, p.Value));
            }
        }

        // Top searches
        var topSearchQueries = stats
            .Where(s => s.MetricName == "searches" && !string.IsNullOrEmpty(s.Dimension))
            .GroupBy(s => s.Dimension!)
            .Select(g => new TopMetricItemDto(g.Key, g.Sum(x => x.MetricValue), null, true))
            .OrderByDescending(x => x.Value)
            .Take(10)
            .ToList();

        // Top empty searches
        var topEmptySearches = stats
            .Where(s => s.MetricName == "empty_searches" && !string.IsNullOrEmpty(s.Dimension))
            .GroupBy(s => s.Dimension!)
            .Select(g => new TopMetricItemDto(g.Key, g.Sum(x => x.MetricValue), null, false))
            .OrderByDescending(x => x.Value)
            .Take(10)
            .ToList();

        // Failing sources from problem reports
        var reportItems = await _db.SourceReports
            .AsNoTracking()
            .Select(r => new {
                Quality = r.Source != null ? r.Source.Quality : "Source",
                r.Reason
            })
            .Take(100)
            .ToListAsync(cancellationToken);

        var failingSources = reportItems
            .GroupBy(r => $"{r.Quality} ({r.Reason})")
            .Select(g => new TopMetricItemDto(g.Key, g.Count(), null, null))
            .OrderByDescending(x => x.Value)
            .Take(8)
            .ToList();

        return new AnalyticsOverviewDto(
            TotalPlays: totalPlays,
            TotalSearches: totalSearches,
            TotalEmptySearches: totalEmptySearches,
            TotalSignups: totalSignups,
            DailyTrends: dailyTrends,
            TopPlayedTitles: topPlayedTitles,
            TopSearchQueries: topSearchQueries,
            TopEmptySearches: topEmptySearches,
            FailingSources: failingSources
        );
    }
}
