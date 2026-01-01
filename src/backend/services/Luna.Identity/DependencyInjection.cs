namespace Luna.Identity;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency injection configuration for Luna.Identity module.
/// Encapsulates all identity-related services and their dependencies.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services)
    {
        // Authentication services will go here
        // JWT token generation, user identity resolution, etc.

        return services;
    }
}
