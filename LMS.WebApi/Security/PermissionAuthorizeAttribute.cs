using Microsoft.AspNetCore.Authorization;

namespace LMS.WebApi.Security;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class PermissionAuthorizeAttribute : AuthorizeAttribute
{
    public const string Prefix = "Permission:";

    public PermissionAuthorizeAttribute(string permission)
    {
        Permission = permission;
        Policy = Prefix + permission;
    }

    public string Permission { get; }
}
