using LMS.Application.Common.Security;
using LMS.WebApi.Security;
using Microsoft.AspNetCore.Authorization;

namespace LMS.WebApi.Extensions;

public static class AuthorizationExtensions
{
    /// <summary>
    /// Wires policies + the dynamic permission-claim provider.
    ///
    /// Permission-claim gates ("Permissions.Students.Read", …) are dynamic and
    /// resolved by <see cref="DynamicPolicyProvider"/>. Only the few role-based
    /// gates still in use are registered explicitly here. The legacy
    /// AcademyDirector/OfficeAdmin/SupportTeacher policies were dropped when
    /// the role surface collapsed to admin/teacher/student.
    /// </summary>
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireTeacher", p => p.RequireRole(RoleCodes.Teacher));
            options.AddPolicy("RequireStudent", p => p.RequireRole(RoleCodes.Student));
            options.AddPolicy("CanManageResults", p => p.RequireRole(RoleCodes.Admin, RoleCodes.SuperAdmin));
        });
        services.AddSingleton<IAuthorizationPolicyProvider, DynamicPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        return services;
    }
}
