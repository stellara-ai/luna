namespace Luna.Students.Persistence;

using Luna.Students.Profiles;

/// <summary>
/// Student profile persistence.
/// </summary>
public interface IStudentRepository
{
    Task<StudentProfile?> GetStudentAsync(string studentId, CancellationToken ct);
    Task SaveStudentAsync(StudentProfile student, CancellationToken ct);
}

public class StudentRepository : IStudentRepository
{
    public async Task<StudentProfile?> GetStudentAsync(string studentId, CancellationToken ct)
    {
        // EF Core query
        await Task.Delay(0, ct);
        throw new NotImplementedException();
    }

    public async Task SaveStudentAsync(StudentProfile student, CancellationToken ct)
    {
        // EF Core save
        await Task.Delay(0, ct);
        throw new NotImplementedException();
    }
}
