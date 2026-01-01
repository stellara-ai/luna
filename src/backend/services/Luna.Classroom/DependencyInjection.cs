namespace Luna.Classroom;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency injection configuration for Luna.Classroom module.
/// Core domain: real-time teaching sessions, orchestration, persistence.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddClassroomModule(this IServiceCollection services)
    {
        // Session orchestration
        // WebSocket handlers
        // Teaching turn execution
        // Event sourcing for session history

        return services;
    }
}
