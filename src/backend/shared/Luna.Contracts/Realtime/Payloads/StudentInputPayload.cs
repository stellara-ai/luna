namespace Luna.Contracts.Realtime.Payloads;

using Luna.Contracts.Realtime;

/// <summary>
/// Student input during a classroom session.
/// </summary>
public class StudentInputPayload : WsPayload
{
    public string Content { get; set; } = string.Empty;
    public InputType Type { get; set; }
}