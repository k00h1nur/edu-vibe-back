using LMS.Application.Common.Security;

namespace LMS.WebApi.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAcademyDirector", p => p.RequireRole(RoleCodes.AcademyDirector));
            options.AddPolicy("RequireOfficeAdmin", p => p.RequireRole(RoleCodes.OfficeAdmin));
            options.AddPolicy("RequireTeacher", p => p.RequireRole(RoleCodes.Teacher));
            options.AddPolicy("RequireSupportTeacher", p => p.RequireRole(RoleCodes.SupportTeacher));
            options.AddPolicy("RequireStudent", p => p.RequireRole(RoleCodes.Student));
        });
        return services;
    }
}