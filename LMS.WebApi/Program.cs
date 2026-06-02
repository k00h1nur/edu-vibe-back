using System.Threading.RateLimiting;
using LMS.Application;
using LMS.Application.Common.Abstractions;
using LMS.Infrastructure;
using LMS.WebApi.Extensions;
using LMS.WebApi.Middleware;
using LMS.WebApi.Security;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Microsoft.Extensions.FileProviders;
using LMS.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));
builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorizationPolicies();
builder.Services.AddSwagger();
builder.Services.AddHostedService<PermissionDiscoveryHostedService>();
// Must run AFTER PermissionDiscoveryHostedService — hosted services start in registration order.
builder.Services.AddHostedService<RolePermissionSeederHostedService>();
// Runs last so roles + permissions are guaranteed seeded before we try to
// attach the demo users to them. Idempotent and config-gated.
builder.Services.AddHostedService<DemoUsersSeederHostedService>();

// CORS — browser clients (marketing site at :5173, LMS admin at :3000) need
// the API origin to whitelist them. Origins are config-driven via
// "Cors:AllowedOrigins"; a sensible localhost default kicks in if the array
// is empty so a fresh checkout works without extra setup. AllowCredentials is
// on so the Next.js admin can send its httpOnly cookie on /me etc.
var corsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>();
if (corsOrigins is null || corsOrigins.Length == 0)
{
    corsOrigins = new[]
    {
        "http://localhost:5173", // Vite (marketing)
        "http://localhost:5174", // Vite fallback port
        "http://localhost:3000", // Next.js (LMS admin)
        "http://localhost:3001",
    };
}
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(corsOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

// Health checks — wires up /health (liveness, always OK if process is up) and
// /health/ready (readiness, also pings the DB so load balancers can wait for warm starts).
builder.Services.AddHealthChecks()
    .AddDbContextCheck<LMSDbContext>("postgres", failureStatus: HealthStatus.Unhealthy);

// Rate limiting — applied selectively via [EnableRateLimiting] on hot anonymous
// endpoints (currently just POST /api/VisitorMessages). 10 requests / minute per
// remote IP, queue depth 0 — over-limit returns 429 immediately.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("visitor-submit", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));

    // Per-IP throttle on anonymous auth endpoints (login/register/refresh) to
    // make credential stuffing and refresh-token brute force expensive.
    // 20 requests / minute is generous for a real user (typo retries, a
    // forgotten-password loop) but kills any automated attack.
    options.AddPolicy("auth-anon", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

var redisConn = builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConn))
{
    builder.Services.AddStackExchangeRedisCache(o => o.Configuration = redisConn);
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<RequestResponseLoggingMiddleware>();
var uploadPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");

if (!Directory.Exists(uploadPath))
{
    Directory.CreateDirectory(uploadPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadPath),
    RequestPath = "/uploads"
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// CORS must come BEFORE auth and rate limiter so preflight OPTIONS requests
// short-circuit cleanly without being rate-limited or rejected as unauthorized.
app.UseCors();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Liveness — always 200 if the process is up. Used by k8s livenessProbe / docker HEALTHCHECK.
app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false,
});
// Readiness — 200 only when the DB and any other deps are reachable. Used by load
// balancers + k8s readinessProbe to know when to send traffic.
app.MapHealthChecks("/health/ready");

app.MapControllers();
app.Run();
