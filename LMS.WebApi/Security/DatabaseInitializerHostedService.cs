using LMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LMS.WebApi.Security;

/// <summary>
/// Applies pending EF migrations on boot, gated by the
/// <c>Database:AutoMigrateOnStartup</c> config flag. Defaults to <c>true</c>
/// in Development (so every developer who pulls a branch with new migrations
/// gets them automatically — no more "42703: column does not exist" surprises)
/// and to <c>false</c> in Production (where migrations should be applied via
/// a release pipeline step, not a process restart).
///
/// Runs <b>first</b> in the hosted-service chain so the seeders that follow
/// see the up-to-date schema.
/// </summary>
public sealed class DatabaseInitializerHostedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    IHostEnvironment env,
    ILogger<DatabaseInitializerHostedService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var defaultEnabled = env.IsDevelopment();
        var enabled = configuration.GetValue("Database:AutoMigrateOnStartup", defaultEnabled);

        if (!enabled)
        {
            logger.LogDebug(
                "Auto-migrate disabled (Database:AutoMigrateOnStartup=false). " +
                "Run `dotnet ef database update` from your release pipeline.");
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LMSDbContext>();

        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count == 0)
        {
            logger.LogDebug("Database schema is up to date — no pending migrations.");
            return;
        }

        logger.LogInformation(
            "Applying {Count} pending migration(s): {Migrations}",
            pending.Count, string.Join(", ", pending));

        try
        {
            await db.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            // We log loudly but rethrow so the host shuts down — running with a
            // half-applied schema is the surest way to corrupt data.
            logger.LogCritical(ex,
                "Failed to apply pending migrations. Process will not start. " +
                "Run `dotnet ef database update -p LMS.Infrastructure -s LMS.WebApi` " +
                "manually and inspect the error.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
