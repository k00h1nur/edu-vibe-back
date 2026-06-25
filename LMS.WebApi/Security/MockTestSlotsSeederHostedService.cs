using LMS.Application.Common.Abstractions;
using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LMS.WebApi.Security;

/// <summary>
/// Seeds a few upcoming mock-test sessions into <c>mock_test_slots</c> the
/// first time the table is empty, so the marketing Mock Test page shows real
/// upcoming dates out of the box. Dates are relative to first-seed time (always
/// in the future). Idempotent — once any slot exists this never runs again, so
/// admin-managed slots are preserved. Disable with
/// <c>MockTestSlots:Seed=false</c>.
/// </summary>
public sealed class MockTestSlotsSeederHostedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<MockTestSlotsSeederHostedService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SeedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Non-critical data seed — log and continue so a seeding failure can't
            // crash the host (migrations + RBAC seeders stay fail-fast).
            logger.LogError(ex, "Mock test slot seeding failed; continuing startup without it.");
        }
    }

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue("MockTestSlots:Seed", true))
        {
            logger.LogDebug("Mock test slot seeding disabled by config.");
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        if (await db.MockTestSlots.AnyAsync(cancellationToken))
        {
            logger.LogDebug("mock_test_slots already populated — skipping seed.");
            return;
        }

        // Tashkent is UTC+5, so a 05:00 UTC start reads as 10:00 local.
        var baseDay = DateTime.UtcNow.Date;
        DateTime AtUtc(int addDays, int utcHour) =>
            DateTime.SpecifyKind(baseDay.AddDays(addDays).AddHours(utcHour), DateTimeKind.Utc);

        var slots = new[]
        {
            new MockTestSlot("IELTS Academic Mock Test", AtUtc(5, 5), "2h 45min", 15, 8, 1, true),
            new MockTestSlot("IELTS General Mock Test", AtUtc(7, 9), "2h 45min", 15, 12, 2, true),
            new MockTestSlot("SAT Full Practice Test", AtUtc(8, 4), "3h 15min", 12, 6, 3, true),
        };
        foreach (var s in slots) await db.MockTestSlots.AddAsync(s, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} mock test slots.", slots.Length);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
