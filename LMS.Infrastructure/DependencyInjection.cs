using System.Text;
using LMS.Application.Common.Abstractions;
using LMS.Infrastructure.Persistence;
using LMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required.");

        // DbContext pooling halves per-request CPU on the change tracker /
        // model snapshot / query cache. EnableRetryOnFailure absorbs transient
        // Postgres blips (connection reset, brief network partition) without
        // bubbling up as 500s.
        services.AddDbContextPool<LMSDbContext>(opt =>
        {
            opt.UseNpgsql(connection, npg =>
            {
                npg.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(2),
                    errorCodesToAdd: null);
                npg.MigrationsAssembly(typeof(LMSDbContext).Assembly.FullName);
            });
            // Warnings as errors for footguns we never want to silently ship.
            opt.ConfigureWarnings(w => w.Throw(
                RelationalEventId.MultipleCollectionIncludeWarning,
                CoreEventId.RowLimitingOperationWithoutOrderByWarning));
        });
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<LMSDbContext>());
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        // No per-request state — singleton avoids per-request key allocation.
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IResultImageService, ResultImageService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddHttpContextAccessor();

        // Telegram pipeline: producer/consumer split.
        //   • TelegramNotifier (singleton) enqueues into a bounded Channel — non-blocking.
        //   • TelegramSenderHostedService drains the channel on one worker with retry +
        //     backoff and honors Telegram's 429 retry_after.
        //   • Notifier is registered both as the interface (for callers) and the
        //     concrete type (so the hosted service can reach the channel reader
        //     without duplicating the singleton).
        services.Configure<TelegramOptions>(configuration.GetSection(TelegramOptions.SectionName));
        services.AddHttpClient("Telegram", (sp, c) =>
        {
            var opts = sp.GetRequiredService<IOptions<TelegramOptions>>().Value;
            c.Timeout = TimeSpan.FromSeconds(Math.Max(1, opts.RequestTimeoutSeconds));
        });
        services.AddSingleton<TelegramNotifier>();
        services.AddSingleton<ITelegramNotifier>(sp => sp.GetRequiredService<TelegramNotifier>());
        services.AddHostedService<TelegramSenderHostedService>();

        services.AddSingleton<ITaskGrader, TaskGrader>();
        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "Jwt:Key is required. Set it via configuration or the Jwt__Key environment variable.");

        var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Key must be at least 32 bytes for HS256.");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    // Defaults to 5 min — too generous for short-lived tokens.
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });
        return services;
    }
}
