namespace Luna.Classroom.Sessions;

using Luna.SharedKernel.Time;

public sealed class ClassroomSession
{
    public string SessionId { get; private set; }
    public string StudentId { get; private set; }
    public string LessonId { get; private set; }

    public DateTime StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }

    public SessionState State { get; private set; }

    public List<SessionEvent> Events { get; } = new();

    private ClassroomSession()
    {
        SessionId = string.Empty;
        StudentId = string.Empty;
        LessonId = string.Empty;
        StartedAt = default;
        State = SessionState.Created;
    }

    public static ClassroomSession Create(string studentId, string lessonId, ISystemClock clock)
    {
        return new ClassroomSession
        {
            SessionId = Guid.NewGuid().ToString("N"),
            StudentId = studentId,
            LessonId = lessonId,
            StartedAt = clock.UtcNow,
            State = SessionState.Active
        };
    }

    public void RecordEvent(SessionEvent evt, ISystemClock clock)
    {
        evt.Sequence = Events.Count + 1;
        if (evt.Timestamp == default)
        {
            evt.Timestamp = clock.UtcNow;
        }

        Events.Add(evt);
    }

    public void End(string reason, ISystemClock clock)
    {
        State = SessionState.Ended;
        EndedAt = clock.UtcNow;

        var evt = new SessionEvent
        {
            Sequence = Events.Count + 1,
            EventType = "session.ended",
            Data = new Dictionary<string, object>
            {
                ["reason"] = reason
            }
        };

        RecordEvent(evt, clock);
    }
}

public enum SessionState
{
    Created,
    Active,
    Paused,
    Ended
}

public sealed class SessionEvent
{
    public int Sequence { get; set; }
    public required string EventType { get; init; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Data { get; set; } = new();
}