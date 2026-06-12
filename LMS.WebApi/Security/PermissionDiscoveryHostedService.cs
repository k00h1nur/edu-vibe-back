using System.Reflection;
using LMS.Application.Common.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;

namespace LMS.WebApi.Security;

public sealed class PermissionDiscoveryHostedService(IServiceProvider serviceProvider, ILogger<PermissionDiscoveryHostedService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // Controller-attribute scan PLUS the static catalog. UI-only
        // capability gates (Analytics.Read, Practice.Read, Reports.Read)
        // appear on no controller — they exist purely to drive nav/feature
        // visibility — so without the catalog union they never reach the
        // permissions table and the role seeder can't grant them.
        var discovered = DiscoverPermissions()
            .Concat(LMS.Application.Common.Security.Permissions.All)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (discovered.Count == 0) return;

        var existing = await db.Permissions.Select(x => x.Code).ToListAsync(cancellationToken);
        var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var code in discovered)
        {
            if (existingSet.Contains(code)) continue;
            var module = code.Contains('.') ? code.Split('.')[0] : "General";
            await db.Permissions.AddAsync(new LMS.Domain.Entities.Permission(code, module, "Auto-discovered"), cancellationToken);
            logger.LogInformation("Discovered permission: {Permission}", code);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static IEnumerable<string> DiscoverPermissions()
    {
        var asm = typeof(Program).Assembly;
        var controllers = asm.GetTypes()
            .Where(t => !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t));

        foreach (var controller in controllers)
        {
            var classPerms = controller.GetCustomAttributes<PermissionAuthorizeAttribute>(true).Select(x => x.Permission).ToArray();
            foreach (var cp in classPerms) yield return cp;

            var methods = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes().Any(a => a is HttpMethodAttribute));

            foreach (var method in methods)
            {
                var explicitPerms = method.GetCustomAttributes<PermissionAuthorizeAttribute>(true).Select(x => x.Permission).ToArray();
                if (explicitPerms.Length > 0)
                {
                    foreach (var ep in explicitPerms) yield return ep;
                    continue;
                }

                var module = controller.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase)
                    ? controller.Name[..^10]
                    : controller.Name;

                var action = method.Name;
                yield return $"{module}.{action}";
            }
        }
    }
}
