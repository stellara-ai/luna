namespace Luna.Contracts.Realtime.Payloads;

using Luna.Contracts.Realtime;

/// <summary>
/// Session lifecycle event.
/// </summary>
public class SessionEventPayload : WsPayload
{
    public string EventType { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
    public int SequenceNumber { get; set; }
}