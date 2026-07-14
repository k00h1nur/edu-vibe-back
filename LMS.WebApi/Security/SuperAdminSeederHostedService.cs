using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Security;
using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using UserRoleEntity = LMS.Domain.Entities.UserRole;

namespace LMS.WebApi.Security;

/// <summary>
/// Promotes a configured account to <see cref="RoleCodes.SuperAdmin"/> on boot so
/// the operator can designate THEIR OWN account as the platform's super-admin
/// without editing seed data or SQL. Set:
///
///   "SuperAdmin": { "Email": "you@yourdomain.com" }
///
/// (or the env var <c>SuperAdmin__Email</c>). On startup, if a user with that
/// email exists and doesn't already hold SuperAdmin, this grants it. Strictly
/// idempotent and additive — it never revokes SuperAdmin from anyone and never
/// touches passwords. If the email is unset it's a no-op (the seeded
/// <c>director@eduvibe.local</c> account already holds SuperAdmin as the
/// bootstrap god-mode login — change its password before exposing prod). If the
/// email is set but no such user exists yet, it logs a clear warning so the
/// operator knows to create the account first, then restart.
///
/// Runs AFTER <see cref="DemoUsersSeederHostedService"/> so a freshly-seeded
/// demo account can be the target in development.
/// </summary>
public sealed class SuperAdminSeederHostedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<SuperAdminSeederHostedService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var email = configuration["SuperAdmin:Email"]?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogInformation(
                "No SuperAdmin:Email configured — leaving super-admin grants as seeded. " +
                "Set SuperAdmin:Email to promote your own account.");
            return;
        }

        email = email.ToLowerInvariant();

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var superRoleId = await db.Roles
            .Where(r => r.Code == RoleCodes.SuperAdmin)
            .Select(r => (Guid?)r.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (superRoleId is null)
        {
            logger.LogWarning("SuperAdmin role row is missing — cannot promote {Email}.", email);
            return;
        }

        var user = await db.Users
            .Where(u => u.Email == email)
            .Select(u => new { u.Id })
            .FirstOrDefaultAsync(cancellationToken);
        if (user is null)
        {
            logger.LogWarning(
                "SuperAdmin:Email is set to {Email} but no such user exists yet. " +
                "Create that account first, then restart to promote it.", email);
            return;
        }

        var alreadyHas = await db.UserRoles
            .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == superRoleId.Value, cancellationToken);
        if (alreadyHas)
        {
            logger.LogInformation("SuperAdmin already granted to {Email} — nothing to do.", email);
            return;
        }

        await db.UserRoles.AddAsync(new UserRoleEntity(user.Id, superRoleId.Value), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Granted SuperAdmin to {Email}.", email);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
