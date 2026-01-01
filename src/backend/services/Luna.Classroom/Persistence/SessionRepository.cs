namespace Luna.Classroom.Persistence;

using Luna.Classroom.Sessions;
using System.Collections.Concurrent;

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
    // In-memory store for first iteration; replace with database persistence later.
    private static readonly ConcurrentDictionary<string, ClassroomSession> Sessions = new(StringComparer.OrdinalIgnoreCase);

    public async Task<ClassroomSession?> GetSessionAsync(string sessionId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Task.Yield();
        Sessions.TryGetValue(sessionId, out var session);
        return session;
    }

    public async Task SaveSessionAsync(ClassroomSession session, CancellationToken ct)
    {
        await Task.Yield();
        Sessions[session.SessionId] = session;
    }

    public async Task<IEnumerable<ClassroomSession>> GetStudentSessionsAsync(string studentId, CancellationToken ct)
    {
        await Task.Yield();
        return Sessions.Values.Where(s => s.StudentId.Equals(studentId, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
