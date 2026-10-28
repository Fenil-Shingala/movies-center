using Hangfire;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MoviesCenter.Domain.Entities.Identity;
using MoviesCenter.Infrastructure.Data;

namespace MoviesCenter.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? configuration["DATABASE_CONNECTION_STRING"]
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=MoviesCenterDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString, b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        var redisConnectionString = configuration["REDIS_CONNECTION_STRING"] ?? configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "MoviesCenter:";
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddSingleton<MoviesCenter.Application.Common.Interfaces.ICacheService, MoviesCenter.Infrastructure.Services.DistributedCacheService>();

        services.AddHttpClient<MoviesCenter.Application.Common.Interfaces.ITmdbService, MoviesCenter.Infrastructure.Services.TmdbService>(client =>
        {
            var baseUrl = configuration["Tmdb:BaseUrl"] ?? "https://api.themoviedb.org/3/";
            if (!baseUrl.EndsWith('/')) baseUrl += "/";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(20);
        });

        services.AddScoped<MoviesCenter.Application.Common.Interfaces.ITmdbSyncService, MoviesCenter.Infrastructure.Services.TmdbSyncService>();
        services.AddScoped<MoviesCenter.Application.Common.Interfaces.ICatalogQueryService, MoviesCenter.Infrastructure.Services.CatalogQueryService>();
        services.AddScoped<MoviesCenter.Application.Common.Interfaces.IJwtTokenService, MoviesCenter.Infrastructure.Services.JwtTokenService>();
        services.AddScoped<MoviesCenter.Application.Common.Interfaces.IAuthService, MoviesCenter.Infrastructure.Services.AuthService>();
        services.AddScoped<MoviesCenter.Application.Common.Interfaces.IUserDataService, MoviesCenter.Infrastructure.Services.UserDataService>();
        services.AddScoped<MoviesCenter.Application.Common.Interfaces.IRatingService, MoviesCenter.Infrastructure.Services.RatingService>();
        services.AddScoped<MoviesCenter.Application.Common.Interfaces.IAdminService, MoviesCenter.Infrastructure.Services.AdminService>();
        services.AddScoped<MoviesCenter.Application.Common.Interfaces.IEmailService, MoviesCenter.Infrastructure.Services.EmailService>();
        services.AddScoped<MoviesCenter.Application.Common.Interfaces.INotificationService, MoviesCenter.Infrastructure.Services.NotificationService>();
        services.AddScoped<MoviesCenter.Application.Common.Interfaces.IPushNotificationService, MoviesCenter.Infrastructure.Services.PushNotificationService>();

        // Hangfire Background Processing
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(Hangfire.CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(connectionString, new Hangfire.SqlServer.SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.Zero,
                UseRecommendedIsolationLevel = true,
                DisableGlobalLocks = true
            }));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 2;
        });

        return services;
    }
}
