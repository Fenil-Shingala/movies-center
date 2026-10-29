using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MoviesCenter.Domain.Constants;
using MoviesCenter.Domain.Entities;
using MoviesCenter.Domain.Entities.Identity;
using MoviesCenter.Infrastructure.Extensions;

namespace MoviesCenter.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<SourceProvider> SourceProviders => Set<SourceProvider>();
    public DbSet<Title> Titles => Set<Title>();
    public DbSet<TitleGenre> TitleGenres => Set<TitleGenre>();
    public DbSet<TitleSource> TitleSources => Set<TitleSource>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<Person> People => Set<Person>();
    public DbSet<TitleCast> TitleCasts => Set<TitleCast>();
    public DbSet<Video> Videos => Set<Video>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<TitleProvider> TitleProviders => Set<TitleProvider>();
    public DbSet<TmdbSyncLog> TmdbSyncLogs => Set<TmdbSyncLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<WatchlistItem> WatchlistItems => Set<WatchlistItem>();
    public DbSet<WatchHistoryItem> WatchHistoryItems => Set<WatchHistoryItem>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<SourceReport> SourceReports => Set<SourceReport>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationSettings> NotificationSettings => Set<NotificationSettings>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<AnalyticsDailyStats> AnalyticsDailyStats => Set<AnalyticsDailyStats>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ReviewLike> ReviewLikes => Set<ReviewLike>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Title
        builder.Entity<Title>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => t.TmdbId).IsUnique();
            entity.HasIndex(t => t.Type);
            entity.HasIndex(t => t.IsFeatured);
            entity.HasIndex(t => t.Popularity);
            entity.HasIndex(t => t.ReleaseDate);
            entity.Property(t => t.TitleText).IsRequired().HasMaxLength(300);
            entity.Property(t => t.OriginalTitle).HasMaxLength(300);
            entity.Property(t => t.Type).IsRequired().HasMaxLength(50);
            entity.Property(t => t.PosterPath).HasMaxLength(500);
            entity.Property(t => t.BackdropPath).HasMaxLength(500);
            entity.Property(t => t.Tagline).HasMaxLength(500);
            entity.Property(t => t.ContentRating).HasMaxLength(20);
            entity.Property(t => t.OriginalLanguage).HasMaxLength(10);
            entity.Property(t => t.Status).HasMaxLength(50);
            entity.Property(t => t.TmdbListCategory).HasMaxLength(200);
            entity.Property(t => t.ImdbRating).HasPrecision(3, 1);
            entity.Property(t => t.TmdbRating).HasPrecision(3, 1);
        });

        // Configure Season
        builder.Entity<Season>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => new { s.TitleId, s.SeasonNumber });
            entity.Property(s => s.Name).IsRequired().HasMaxLength(200);
            entity.Property(s => s.PosterPath).HasMaxLength(500);
            entity.HasOne(s => s.Title)
                  .WithMany(t => t.Seasons)
                  .HasForeignKey(s => s.TitleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Episode
        builder.Entity<Episode>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.SeasonId, e.EpisodeNumber });
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.StillPath).HasMaxLength(500);
            entity.HasOne(e => e.Season)
                  .WithMany(s => s.Episodes)
                  .HasForeignKey(e => e.SeasonId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Person
        builder.Entity<Person>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.TmdbId).IsUnique();
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.ProfilePath).HasMaxLength(500);
        });

        // Configure TitleCast
        builder.Entity<TitleCast>(entity =>
        {
            entity.HasKey(tc => tc.Id);
            entity.HasIndex(tc => new { tc.TitleId, tc.PersonId });
            entity.Property(tc => tc.Character).HasMaxLength(200);
            entity.HasOne(tc => tc.Title)
                  .WithMany(t => t.TitleCasts)
                  .HasForeignKey(tc => tc.TitleId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(tc => tc.Person)
                  .WithMany(p => p.TitleCasts)
                  .HasForeignKey(tc => tc.PersonId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Video
        builder.Entity<Video>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.HasIndex(v => v.TitleId);
            entity.Property(v => v.TmdbKey).IsRequired().HasMaxLength(100);
            entity.Property(v => v.Name).HasMaxLength(200);
            entity.Property(v => v.Type).HasMaxLength(50);
            entity.Property(v => v.Site).HasMaxLength(50);
            entity.HasOne(v => v.Title)
                  .WithMany(t => t.Videos)
                  .HasForeignKey(v => v.TitleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Provider
        builder.Entity<Provider>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.TmdbId).IsUnique();
            entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
            entity.Property(p => p.LogoPath).HasMaxLength(500);
        });

        // Configure TitleProvider
        builder.Entity<TitleProvider>(entity =>
        {
            entity.HasKey(tp => tp.Id);
            entity.HasIndex(tp => new { tp.TitleId, tp.ProviderId });
            entity.Property(tp => tp.Type).HasMaxLength(50);
            entity.Property(tp => tp.Country).HasMaxLength(10);
            entity.HasOne(tp => tp.Title)
                  .WithMany(t => t.TitleProviders)
                  .HasForeignKey(tp => tp.TitleId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(tp => tp.Provider)
                  .WithMany(p => p.TitleProviders)
                  .HasForeignKey(tp => tp.ProviderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure TmdbSyncLog
        builder.Entity<TmdbSyncLog>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.HasIndex(l => l.StartedAt);
            entity.Property(l => l.Status).IsRequired().HasMaxLength(50);
            entity.Property(l => l.TriggeredBy).IsRequired().HasMaxLength(50);
        });

        // Configure Genre
        builder.Entity<Genre>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Id).ValueGeneratedNever(); // TMDB ID
            entity.Property(g => g.Name).IsRequired().HasMaxLength(100);
            entity.Property(g => g.Slug).IsRequired().HasMaxLength(100);
            entity.HasIndex(g => g.Slug).IsUnique();
        });

        // Configure TitleGenre (many-to-many join)
        builder.Entity<TitleGenre>(entity =>
        {
            entity.HasKey(tg => new { tg.TitleId, tg.GenreId });
            entity.HasOne(tg => tg.Title)
                  .WithMany(t => t.TitleGenres)
                  .HasForeignKey(tg => tg.TitleId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(tg => tg.Genre)
                  .WithMany(g => g.TitleGenres)
                  .HasForeignKey(tg => tg.GenreId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure SourceProvider
        builder.Entity<SourceProvider>(entity =>
        {
            entity.HasKey(sp => sp.Id);
            entity.Property(sp => sp.Name).IsRequired().HasMaxLength(100);
            entity.Property(sp => sp.Slug).IsRequired().HasMaxLength(100);
            entity.Property(sp => sp.Type).IsRequired().HasMaxLength(50);
            entity.Property(sp => sp.BaseUrl).HasMaxLength(500);
            entity.Property(sp => sp.EmbedUrlTemplate).HasMaxLength(500);
            entity.HasIndex(sp => sp.Slug).IsUnique();
        });

        // Configure TitleSource
        builder.Entity<TitleSource>(entity =>
        {
            entity.HasKey(ts => ts.Id);
            entity.Property(ts => ts.Quality).HasMaxLength(20);
            entity.Property(ts => ts.EmbedUrl).HasMaxLength(1000);
            entity.Property(ts => ts.StreamUrl).HasMaxLength(1000);
            entity.HasOne(ts => ts.Title)
                  .WithMany(t => t.TitleSources)
                  .HasForeignKey(ts => ts.TitleId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ts => ts.SourceProvider)
                  .WithMany(sp => sp.TitleSources)
                  .HasForeignKey(ts => ts.SourceProviderId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(ts => new { ts.TitleId, ts.SourceProviderId, ts.SeasonNumber, ts.EpisodeNumber });
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(rt => rt.Id);
            entity.HasIndex(rt => rt.Token);
            entity.HasIndex(rt => rt.UserId);
            entity.Property(rt => rt.Token).IsRequired().HasMaxLength(500);
            entity.HasOne(rt => rt.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(rt => rt.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WatchlistItem>(entity =>
        {
            entity.HasKey(w => w.Id);
            entity.HasIndex(w => new { w.UserId, w.TitleId }).IsUnique();
            entity.HasOne(w => w.User)
                  .WithMany(u => u.WatchlistItems)
                  .HasForeignKey(w => w.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(w => w.Title)
                  .WithMany()
                  .HasForeignKey(w => w.TitleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<WatchHistoryItem>(entity =>
        {
            entity.HasKey(h => h.Id);
            entity.HasIndex(h => new { h.UserId, h.TitleId, h.EpisodeId });
            entity.HasOne(h => h.User)
                  .WithMany(u => u.WatchHistory)
                  .HasForeignKey(h => h.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(h => h.Title)
                  .WithMany()
                  .HasForeignKey(h => h.TitleId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(h => h.Episode)
                  .WithMany()
                  .HasForeignKey(h => h.EpisodeId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Rating>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => new { r.UserId, r.TitleId }).IsUnique();
            entity.Property(r => r.Score).IsRequired();
            entity.HasOne(r => r.User)
                  .WithMany(u => u.Ratings)
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.Title)
                  .WithMany(t => t.Ratings)
                  .HasForeignKey(r => r.TitleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SourceReport>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.Status);
            entity.HasIndex(r => r.CreatedAt);
            entity.Property(r => r.Reason).IsRequired().HasMaxLength(100);
            entity.Property(r => r.Status).IsRequired().HasMaxLength(50);
            entity.Property(r => r.Details).HasMaxLength(1000);
            entity.HasOne(r => r.Title)
                  .WithMany()
                  .HasForeignKey(r => r.TitleId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.Source)
                  .WithMany()
                  .HasForeignKey(r => r.SourceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });
            entity.Property(n => n.Type).IsRequired().HasMaxLength(100);
            entity.Property(n => n.Title).IsRequired().HasMaxLength(200);
            entity.Property(n => n.Body).IsRequired().HasMaxLength(1000);
            entity.HasOne(n => n.User)
                  .WithMany(u => u.Notifications)
                  .HasForeignKey(n => n.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<NotificationSettings>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.UserId).IsUnique();
            entity.HasOne(s => s.User)
                  .WithOne(u => u.NotificationSettings)
                  .HasForeignKey<NotificationSettings>(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PushSubscription>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.UserId);
            entity.Property(p => p.Endpoint).IsRequired().HasMaxLength(1000);
            entity.Property(p => p.P256dh).IsRequired().HasMaxLength(500);
            entity.Property(p => p.Auth).IsRequired().HasMaxLength(200);
            entity.Property(p => p.UserAgent).HasMaxLength(500);
            entity.HasOne(p => p.User)
                  .WithMany(u => u.PushSubscriptions)
                  .HasForeignKey(p => p.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Review>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.TitleId);
            entity.HasIndex(r => r.UserId);
            entity.HasIndex(r => new { r.TitleId, r.CreatedAt });
            entity.Property(r => r.Content).IsRequired().HasMaxLength(2000);
            entity.HasOne(r => r.User)
                  .WithMany(u => u.Reviews)
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.Title)
                  .WithMany(t => t.Reviews)
                  .HasForeignKey(r => r.TitleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ReviewLike>(entity =>
        {
            entity.HasKey(rl => rl.Id);
            entity.HasIndex(rl => new { rl.ReviewId, rl.UserId }).IsUnique();
            entity.HasOne(rl => rl.Review)
                  .WithMany(r => r.ReviewLikes)
                  .HasForeignKey(rl => rl.ReviewId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(rl => rl.User)
                  .WithMany(u => u.ReviewLikes)
                  .HasForeignKey(rl => rl.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Convert table names and column names to snake_case
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            var tableName = entity.GetTableName();
            if (!string.IsNullOrEmpty(tableName))
            {
                // Remove AspNet prefix if present and convert to snake_case
                var cleanName = tableName.StartsWith("AspNet", StringComparison.OrdinalIgnoreCase)
                    ? tableName["AspNet".Length..]
                    : tableName;
                entity.SetTableName(cleanName.ToSnakeCase());
            }

            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(property.Name.ToSnakeCase());
            }

            foreach (var key in entity.GetKeys())
            {
                key.SetName(key.GetName()?.ToSnakeCase());
            }

            foreach (var key in entity.GetForeignKeys())
            {
                key.SetConstraintName(key.GetConstraintName()?.ToSnakeCase());
            }

            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(index.GetDatabaseName()?.ToSnakeCase());
            }
        }

        // Seed initial data
        SeedInitialData(builder);
    }

    private static void SeedInitialData(ModelBuilder builder)
    {
        var adminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var userRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var adminUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var seedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Seed Roles
        builder.Entity<ApplicationRole>().HasData(
            new ApplicationRole(AppConstants.Roles.Admin, "System Administrator")
            {
                Id = adminRoleId,
                NormalizedName = AppConstants.Roles.Admin.ToUpperInvariant(),
                ConcurrencyStamp = adminRoleId.ToString(),
                CreatedAt = seedDate
            },
            new ApplicationRole(AppConstants.Roles.User, "Registered User")
            {
                Id = userRoleId,
                NormalizedName = AppConstants.Roles.User.ToUpperInvariant(),
                ConcurrencyStamp = userRoleId.ToString(),
                CreatedAt = seedDate
            }
        );

        // Seed Admin User (admin.movies@gmail.com / Admin#movies@123)
        var hasher = new PasswordHasher<ApplicationUser>();
        var adminUser = new ApplicationUser
        {
            Id = adminUserId,
            UserName = "admin.movies@gmail.com",
            NormalizedUserName = "ADMIN.MOVIES@GMAIL.COM",
            Email = "admin.movies@gmail.com",
            NormalizedEmail = "ADMIN.MOVIES@GMAIL.COM",
            EmailConfirmed = true,
            SecurityStamp = "STATIC_SECURITY_STAMP_SEED_2026",
            ConcurrencyStamp = adminUserId.ToString(),
            IsActive = true,
            CreatedAt = seedDate,
            UpdatedAt = seedDate
        };
        adminUser.PasswordHash = hasher.HashPassword(adminUser, "Admin#movies@123");

        builder.Entity<ApplicationUser>().HasData(adminUser);

        // Seed UserRole join
        builder.Entity<IdentityUserRole<Guid>>().HasData(new IdentityUserRole<Guid>
        {
            UserId = adminUserId,
            RoleId = adminRoleId
        });

        // Seed VidSrc Provider
        builder.Entity<SourceProvider>().HasData(new SourceProvider
        {
            Id = 1,
            Name = "VidSrc",
            Slug = AppConstants.Providers.VidSrc,
            Type = AppConstants.SourceProviderTypes.ThirdPartyEmbed,
            BaseUrl = "https://vidsrc.to",
            EmbedUrlTemplate = "https://vidsrc.to/embed/movie/{tmdbId}",
            Priority = 1,
            IsActive = true,
            CreatedAt = seedDate,
            UpdatedAt = seedDate
        });

        // Seed TMDB Standard Genres
        builder.Entity<Genre>().HasData(
            new Genre { Id = 28, Name = "Action", Slug = "action", CreatedAt = seedDate },
            new Genre { Id = 12, Name = "Adventure", Slug = "adventure", CreatedAt = seedDate },
            new Genre { Id = 16, Name = "Animation", Slug = "animation", CreatedAt = seedDate },
            new Genre { Id = 35, Name = "Comedy", Slug = "comedy", CreatedAt = seedDate },
            new Genre { Id = 80, Name = "Crime", Slug = "crime", CreatedAt = seedDate },
            new Genre { Id = 99, Name = "Documentary", Slug = "documentary", CreatedAt = seedDate },
            new Genre { Id = 18, Name = "Drama", Slug = "drama", CreatedAt = seedDate },
            new Genre { Id = 10751, Name = "Family", Slug = "family", CreatedAt = seedDate },
            new Genre { Id = 14, Name = "Fantasy", Slug = "fantasy", CreatedAt = seedDate },
            new Genre { Id = 36, Name = "History", Slug = "history", CreatedAt = seedDate },
            new Genre { Id = 27, Name = "Horror", Slug = "horror", CreatedAt = seedDate },
            new Genre { Id = 10402, Name = "Music", Slug = "music", CreatedAt = seedDate },
            new Genre { Id = 9648, Name = "Mystery", Slug = "mystery", CreatedAt = seedDate },
            new Genre { Id = 10749, Name = "Romance", Slug = "romance", CreatedAt = seedDate },
            new Genre { Id = 878, Name = "Science Fiction", Slug = "science-fiction", CreatedAt = seedDate },
            new Genre { Id = 10770, Name = "TV Movie", Slug = "tv-movie", CreatedAt = seedDate },
            new Genre { Id = 53, Name = "Thriller", Slug = "thriller", CreatedAt = seedDate },
            new Genre { Id = 10752, Name = "War", Slug = "war", CreatedAt = seedDate },
            new Genre { Id = 37, Name = "Western", Slug = "western", CreatedAt = seedDate },
            new Genre { Id = 10759, Name = "Action & Adventure", Slug = "action-adventure", CreatedAt = seedDate },
            new Genre { Id = 10762, Name = "Kids", Slug = "kids", CreatedAt = seedDate },
            new Genre { Id = 10763, Name = "News", Slug = "news", CreatedAt = seedDate },
            new Genre { Id = 10764, Name = "Reality", Slug = "reality", CreatedAt = seedDate },
            new Genre { Id = 10765, Name = "Sci-Fi & Fantasy", Slug = "sci-fi-fantasy", CreatedAt = seedDate },
            new Genre { Id = 10766, Name = "Soap", Slug = "soap", CreatedAt = seedDate },
            new Genre { Id = 10767, Name = "Talk", Slug = "talk", CreatedAt = seedDate },
            new Genre { Id = 10768, Name = "War & Politics", Slug = "war-politics", CreatedAt = seedDate }
        );

        // Configure AnalyticsDailyStats
        builder.Entity<AnalyticsDailyStats>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.Date);
            entity.HasIndex(a => new { a.Date, a.MetricName });
            entity.Property(a => a.MetricName).IsRequired().HasMaxLength(100);
            entity.Property(a => a.Dimension).HasMaxLength(500);
        });
    }
}
