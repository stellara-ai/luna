namespace Luna.Classroom;

using Microsoft.Extensions.DependencyInjection;
using Luna.Classroom.Orchestration;
using Luna.Classroom.Persistence;
using Luna.Classroom.Realtime;


public static class DependencyInjection
{
    public static IServiceCollection AddClassroomModule(this IServiceCollection services)
    {
        // Classroom-owned services only
        services.AddScoped<TeachingOrchestrator>();
        services.AddScoped<ClassroomWebSocketHandler>();
        services.AddScoped<ISessionRepository, SessionRepository>();

        return services;
    }
}
