namespace Luna.Curriculum.Persistence;

using Luna.Curriculum.Lessons;

/// <summary>
/// Lesson persistence.
/// </summary>
public interface ILessonRepository
{
    Task<Lesson?> GetLessonAsync(string lessonId, CancellationToken ct);
    Task<IEnumerable<Lesson>> GetSubjectLessonsAsync(string subject, CancellationToken ct);
}

public class LessonRepository : ILessonRepository
{
    public async Task<Lesson?> GetLessonAsync(string lessonId, CancellationToken ct)
    {
        // EF Core query
        await Task.Delay(0, ct);
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<Lesson>> GetSubjectLessonsAsync(string subject, CancellationToken ct)
    {
        // EF Core query
        await Task.Delay(0, ct);
        throw new NotImplementedException();
    }
}
