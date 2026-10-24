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
            entity.Property(t => t.TitleText).IsRequired().HasMaxLength(300);
            entity.Property(t => t.OriginalTitle).HasMaxLength(300);
            entity.Property(t => t.Type).IsRequired().HasMaxLength(50);
            entity.Property(t => t.PosterPath).HasMaxLength(500);
            entity.Property(t => t.BackdropPath).HasMaxLength(500);
            entity.Property(t => t.ContentRating).HasMaxLength(20);
            entity.Property(t => t.OriginalLanguage).HasMaxLength(10);
            entity.Property(t => t.Status).HasMaxLength(50);
            entity.Property(t => t.ImdbRating).HasPrecision(3, 1);
            entity.Property(t => t.TmdbRating).HasPrecision(3, 1);
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
    }
}
