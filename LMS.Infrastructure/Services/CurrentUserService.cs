using System.Security.Claims;
using LMS.Application.Common.Abstractions;
using Microsoft.AspNetCore.Http;

namespace LMS.Infrastructure.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UserId => TryReadGuid("userId");

    public string? Email => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email) ??
                            httpContextAccessor.HttpContext?.User.FindFirstValue("email");

    public IReadOnlyCollection<string> Roles =>
        httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(x => x.Value).ToArray() ?? [];

    public Guid? StudentProfileId => TryReadGuid("studentProfileId");

    public Guid? StaffProfileId => TryReadGuid("staffProfileId");

    public bool IsInRole(string role)
    {
        return Roles.Contains(role);
    }

    private Guid? TryReadGuid(string claimType)
    {
        var raw = httpContextAccessor.HttpContext?.User.FindFirstValue(claimType);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
