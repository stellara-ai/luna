namespace Luna.Contracts.Realtime.Payloads;

using Luna.Contracts.Realtime;
/// <summary>
/// Session end notification.
/// </summary>
public class SessionEndPayload : WsPayload
{
    public string Reason { get; set; } = string.Empty;
    public int TotalDurationSeconds { get; set; }
    public Dictionary<string, object>? SessionMetadata { get; set; }
}