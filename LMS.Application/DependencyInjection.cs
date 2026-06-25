using FluentValidation;
using LMS.Application.Common.Behaviors;
using LMS.Application.Features.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace LMS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        // F3↔F4: shared idempotent lesson-task materializer (manual endpoint + auto-on-generate).
        services.AddScoped<ILessonTaskMaterializer, LessonTaskMaterializer>();
        return services;
    }
}