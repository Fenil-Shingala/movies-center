using System.Text;
using System.Threading.RateLimiting;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MoviesCenter.Application;
using MoviesCenter.Domain.Constants;
using MoviesCenter.Infrastructure;
using MoviesCenter.Infrastructure.Data;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// OpenAPI 
builder.Services.AddOpenApi();

// Clean Architecture services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// JWT Authentication
var jwtSecret = builder.Configuration["JWT_SECRET"] 
    ?? builder.Configuration["Jwt:Secret"] 
    ?? "super_secure_movies_center_jwt_secret_key_development_only_must_be_long_and_random_123456789";
var jwtIssuer = builder.Configuration["JWT_ISSUER"] ?? builder.Configuration["Jwt:Issuer"] ?? "https://localhost:5001";
var jwtAudience = builder.Configuration["JWT_AUDIENCE"] ?? builder.Configuration["Jwt:Audience"] ?? "http://localhost:4200";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Set to true in production
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(AppConstants.Roles.Admin));
});

// CORS Configuration
var corsOrigins = (builder.Configuration["CORS_ORIGINS"] ?? "http://localhost:4200,https://localhost:5001")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/openapi/v1.json", $"{AppConstants.AppName} API v1");
        c.RoutePrefix = "swagger";
    });
}

// Security Headers Middleware
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    await next();
});

app.UseSerilogRequestLogging();
app.UseCors("DefaultPolicy");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health Check Endpoint
app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Application = AppConstants.AppName,
    Version = AppConstants.AppVersion,
    TimestampUtc = DateTime.UtcNow
})).AllowAnonymous();

// Run migrations and seed data on startup if database is available
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // EnsureCreated / Migrate
    if (dbContext.Database.IsSqlServer())
    {
        dbContext.Database.Migrate();
    }
}
catch (Exception ex)
{
    Log.Warning(ex, "Could not apply database migrations on startup. Please ensure SQL Server is running.");
}

// Hangfire is disabled unless Hangfire:Enabled=true
if (builder.Configuration.GetValue<bool>("Hangfire:Enabled"))
{
    try
    {
        app.UseHangfireDashboard("/admin/hangfire", new Hangfire.DashboardOptions
        {
            Authorization = [] // TODO: add an admin-only filter before enabling in production
        });

        var cronSchedule = builder.Configuration["TmdbSync:CronSchedule"] ?? "0 3 * * *";
        Hangfire.RecurringJob.AddOrUpdate<MoviesCenter.Application.Common.Interfaces.ITmdbSyncService>(
            "daily-tmdb-sync",
            service => service.RunFullSyncAsync("HangfireJob", CancellationToken.None),
            cronSchedule
        );
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Could not initialize Hangfire dashboard or recurring job.");
    }
}

app.Run();

// Required for WebApplicationFactory in integration tests
public partial class Program { }
