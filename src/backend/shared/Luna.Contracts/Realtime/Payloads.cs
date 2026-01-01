namespace Luna.Contracts.Realtime.Payloads;

using Luna.Contracts.Realtime;

/// <summary>
/// Student input during a classroom session.
/// </summary>
public class StudentInputPayload : WsPayload
{
    public string Content { get; set; } = string.Empty;
    public InputType Type { get; set; }
    public ControlSignal? ControlSignal { get; set; }
}

public enum InputType
{
    Text,
    Voice,
    GestureControl
}

/// <summary>
/// Teacher response streaming to student.
/// </summary>
public class TeacherResponsePayload : WsPayload
{
    public string Content { get; set; } = string.Empty;
    public ResponseType Type { get; set; }
    public string? AudioChunkBase64 { get; set; }
    public bool IsStreaming { get; set; }
}

public enum ResponseType
{
    TextExplanation,
    AudioStream,
    Question,
    Feedback,
    Encouragement
}

/// <summary>
/// Session lifecycle event.
/// </summary>
public class SessionEventPayload : WsPayload
{
    public string EventType { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public int SequenceNumber { get; set; }
}

/// <summary>
/// Session start request from client.
/// </summary>
public class SessionStartPayload : WsPayload
{
    public string LessonId { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public Dictionary<string, object>? AdaptationContext { get; set; }
}

/// <summary>
/// Session end notification.
/// </summary>
public class SessionEndPayload : WsPayload
{
    public string Reason { get; set; } = string.Empty;
    public int TotalDurationSeconds { get; set; }
    public Dictionary<string, object>? SessionMetadata { get; set; }
}
