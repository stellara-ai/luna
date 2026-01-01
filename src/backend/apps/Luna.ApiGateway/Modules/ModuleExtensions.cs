namespace Luna.ApiGateway.Modules;

using Luna.Identity;
using Luna.Classroom;
using Luna.Students;
using Luna.Curriculum;
using Luna.Media;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Module registration orchestration.
/// Each module independently declares its dependencies via DependencyInjection.cs.
/// This file ensures all modules are registered with the host.
/// </summary>
public static class ModuleExtensions
{
    public static IServiceCollection AddAllModules(this IServiceCollection services, IConfiguration config)
    {
        services
            .AddIdentityModule(config)
            .AddClassroomModule()
            .AddStudentsModule()
            .AddCurriculumModule()
            .AddMediaModule();

        return services;
    }
}
