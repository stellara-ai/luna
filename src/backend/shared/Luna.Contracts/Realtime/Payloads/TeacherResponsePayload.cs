namespace Luna.Contracts.Realtime.Payloads;

using Luna.Contracts.Realtime;

/// <summary>
/// Teacher response streaming to student.
/// </summary>
public class TeacherResponsePayload : WsPayload
{
    public string Content { get; set; } = string.Empty;
    public ResponseType Type { get; set; }
    public string? AudioChunkBase64 { get; set; }
    public bool IsStreaming { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}