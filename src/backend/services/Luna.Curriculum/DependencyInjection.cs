namespace Luna.Curriculum;

using Microsoft.Extensions.DependencyInjection;
using Luna.Curriculum.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddCurriculumModule(this IServiceCollection services)
    {
        services.AddScoped<ILessonRepository, LessonRepository>();

        return services;
    }
}