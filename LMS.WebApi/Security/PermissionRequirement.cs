using Microsoft.AspNetCore.Authorization;

namespace LMS.WebApi.Security;

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;
