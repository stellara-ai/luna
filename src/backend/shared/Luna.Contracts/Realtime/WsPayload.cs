namespace Luna.Contracts.Realtime;
/// <summary>
/// Base class for WebSocket message payloads.
/// All real-time messages must inherit from this.
/// </summary>
public abstract class WsPayload
{
    public string? SessionId { get; set; }
    public string? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
}