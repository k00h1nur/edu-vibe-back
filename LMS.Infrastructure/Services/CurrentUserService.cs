using System.Security.Claims;
using LMS.Application.Common.Abstractions;
using Microsoft.AspNetCore.Http;

namespace LMS.Infrastructure.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UserId => Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirstValue("userId"), out var id)
        ? id
        : null;

    public string? Email => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email) ??
                            httpContextAccessor.HttpContext?.User.FindFirstValue("email");

    public IReadOnlyCollection<string> Roles =>
        httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(x => x.Value).ToArray() ?? [];

    public bool IsInRole(string role)
    {
        return Roles.Contains(role);
    }
}