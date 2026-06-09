using System.Threading.RateLimiting;
using LMS.Application;
using LMS.Infrastructure;
using LMS.Infrastructure.Persistence;
using LMS.WebApi.Extensions;
using LMS.WebApi.Middleware;
using LMS.WebApi.Security;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---- Logging --------------------------------------------------------------
builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

// ---- JSON contract --------------------------------------------------------
// NOTE: enum-as-string would be a more durable contract (renumbering won't
// break clients), but the LMS admin frontend types use numeric enum constants
// today, so flipping this here would be a coordinated breaking change. Track
// as a future migration.
builder.Services.AddControllers();

// ---- Layer registration ---------------------------------------------------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorizationPolicies();
builder.Services.AddSwagger();

// ---- Hosted services (boot order matters) ---------------------------------
// DatabaseInitializerHostedService applies any pending EF migrations first
// (Development default) so the rest of the chain doesn't query a schema that
// doesn't exist yet. PermissionDiscoveryHostedService follows so the
// Permission rows exist before RolePermissionSeederHostedService maps
// role→permission. The demo-user seeder runs last so role + permission
// lookups succeed.
builder.Services.AddHostedService<DatabaseInitializerHostedService>();
builder.Services.AddHostedService<PermissionDiscoveryHostedService>();
builder.Services.AddHostedService<RolePermissionSeederHostedService>();
builder.Services.AddHostedService<DemoUsersSeederHostedService>();

// ---- CORS -----------------------------------------------------------------
// Browser clients (marketing site at :5173, LMS admin at :3000) need the API
// origin to whitelist them. Origins are config-driven via
// "Cors:AllowedOrigins"; a sensible localhost default kicks in in dev so a
// fresh checkout works without extra setup. AllowCredentials is on so the
// Next.js admin can send its httpOnly cookie on /me etc.
//
// In Production / Staging we refuse to boot with an empty origin list —
// silently falling back to localhost would mask a misconfigured deployment.
var corsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>();
if (corsOrigins is null || corsOrigins.Length == 0)
{
    if (!builder.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "Cors:AllowedOrigins must be configured outside Development. " +
            "Set it via configuration or the Cors__AllowedOrigins__0 environment variable.");
    }
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

// ---- Production-only secret checks ----------------------------------------
// In Production we want loud failures at boot rather than runtime — a missing
// Telegram token or empty JWT key should kill the deploy, not silently degrade.
if (builder.Environment.IsProduction())
{
    foreach (var required in new[] { "Jwt:Key", "Telegram:BotToken", "Telegram:ChatId" })
    {
        if (string.IsNullOrWhiteSpace(builder.Configuration[required]))
            throw new InvalidOperationException(
                $"{required} must be set in Production. " +
                $"Use env var {required.Replace(':', '_')}__ or user-secrets / vault.");
    }
}

// ---- Request size limits --------------------------------------------------
// Default Kestrel cap is 30 MB; the Result image endpoint accepts at most
// a few MB. Trimming this prevents a malicious caller from OOM-ing the
// process with a chunked upload.
const int RequestBodyLimit = 10 * 1024 * 1024; // 10 MB
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = RequestBodyLimit);
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = RequestBodyLimit);

// ---- Health checks --------------------------------------------------------
// Wires up /health (liveness, always OK if process is up) and /health/ready
// (readiness, also pings the DB so load balancers can wait for warm starts).
builder.Services.AddHealthChecks()
    .AddDbContextCheck<LMSDbContext>("postgres", failureStatus: HealthStatus.Unhealthy);

// ---- Rate limiting --------------------------------------------------------
// Applied selectively via [EnableRateLimiting] on hot anonymous endpoints.
// 10 r/m on visitor submissions, 20 r/m on auth endpoints. Queue depth 0 —
// over-limit returns 429 immediately.
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

// ---- Caching --------------------------------------------------------------
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

// ---- Middleware pipeline --------------------------------------------------
app.UseMiddleware<GlobalExceptionMiddleware>();

// Serilog request logging: one structured line per request with status code,
// elapsed ms, user id, trace id. Replaces the old ad-hoc body logger so we
// don't accidentally log credentials on /api/Auth/login.
app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (diag, http) =>
    {
        diag.Set("UserId", http.User.FindFirst("userId")?.Value ?? "anon");
        diag.Set("TraceId", http.TraceIdentifier);
        diag.Set("RemoteIp", http.Connection.RemoteIpAddress?.ToString() ?? "unknown");
    };
});

// ---- Static files ---------------------------------------------------------
// Only known media types are served. ServeUnknownFileTypes is OFF so a hostile
// file dropped into uploads/ won't be returned as text/plain etc. Cache-Control
// is set so the result-image URLs CDN-cache properly.
var uploadPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
if (!Directory.Exists(uploadPath))
{
    Directory.CreateDirectory(uploadPath);
}

var allowedMimes = new FileExtensionContentTypeProvider();
allowedMimes.Mappings.Clear();
allowedMimes.Mappings[".jpg"] = "image/jpeg";
allowedMimes.Mappings[".jpeg"] = "image/jpeg";
allowedMimes.Mappings[".png"] = "image/png";
allowedMimes.Mappings[".webp"] = "image/webp";
allowedMimes.Mappings[".pdf"] = "application/pdf";

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadPath),
    RequestPath = "/uploads",
    ContentTypeProvider = allowedMimes,
    ServeUnknownFileTypes = false,
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.CacheControl = "public,max-age=86400";
        ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    },
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
