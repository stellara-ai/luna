namespace Luna.Classroom.Persistence;

using Luna.Classroom.Sessions;

/// <summary>
/// Persistence for classroom sessions.
/// Owns session lifecycle: creation, state updates, event storage.
/// </summary>
public interface ISessionRepository
{
    Task<ClassroomSession?> GetSessionAsync(string sessionId, CancellationToken ct);
    Task SaveSessionAsync(ClassroomSession session, CancellationToken ct);
    Task<IEnumerable<ClassroomSession>> GetStudentSessionsAsync(string studentId, CancellationToken ct);
}

public class SessionRepository : ISessionRepository
{
    public async Task<ClassroomSession?> GetSessionAsync(string sessionId, CancellationToken ct)
    {
        // EF Core query
        await Task.Delay(0, ct);
        throw new NotImplementedException();
    }

    public async Task SaveSessionAsync(ClassroomSession session, CancellationToken ct)
    {
        // EF Core save
        await Task.Delay(0, ct);
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<ClassroomSession>> GetStudentSessionsAsync(string studentId, CancellationToken ct)
    {
        // EF Core query
        await Task.Delay(0, ct);
        throw new NotImplementedException();
    }
}
