namespace Luna.Media;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Dependency injection configuration for Luna.Media module.
/// TTS/STT provider abstraction, streaming audio orchestration.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddMediaModule(this IServiceCollection services)
    {
        // TTS service interface + providers
        // STT service interface + providers
        // Audio streaming orchestration

        return services;
    }
}
