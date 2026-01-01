namespace Luna.Students;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency injection configuration for Luna.Students module.
/// Student profiles, learning preferences, ADHD accommodations.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddStudentsModule(this IServiceCollection services)
    {
        // Student profile repository
        // Learning preferences service
        // Attention metadata tracking

        return services;
    }
}
