namespace Luna.Classroom.Sessions;

/// <summary>
/// Active classroom session state.
/// Ephemeral during lesson; stored for history and auditability.
/// </summary>
public sealed class ClassroomSession
{
    public string SessionId { get; set; }
    public string StudentId { get; set; }
    public string LessonId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public SessionState State { get; set; }
    public List<SessionEvent> Events { get; set; } = new();

    private ClassroomSession() { }

    public static ClassroomSession Create(string studentId, string lessonId) =>
        new()
        {
            SessionId = Guid.NewGuid().ToString("N"),
            StudentId = studentId,
            LessonId = lessonId,
            StartedAt = DateTime.UtcNow,
            State = SessionState.Active
        };

    public void RecordEvent(SessionEvent evt) => Events.Add(evt);

    public void End(string reason)
    {
        State = SessionState.Ended;
        EndedAt = DateTime.UtcNow;
    }
}

public enum SessionState
{
    Created,
    Active,
    Paused,
    Ended
}

/// <summary>
/// Immutable session event for append-only audit log.
/// </summary>
public sealed class SessionEvent
{
    public int Sequence { get; set; }
    public string EventType { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}
