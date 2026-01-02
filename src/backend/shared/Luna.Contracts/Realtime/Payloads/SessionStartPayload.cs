namespace Luna.Contracts.Realtime.Payloads;

using Luna.Contracts.Realtime;

/// <summary>
/// Session start request from client.
/// </summary>
public class SessionStartPayload : WsPayload
{
    public string LessonId { get; set; } = string.Empty;
    public string StudentId { get; set; } = string.Empty;
    public Dictionary<string, object>? AdaptationContext { get; set; }
}