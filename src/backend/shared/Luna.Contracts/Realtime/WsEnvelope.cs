namespace Luna.Contracts.Realtime;

/// <summary>
/// WebSocket message envelope for real-time communication.
/// Provides typed, correlation-tracked message passing.
/// </summary>
public record WsEnvelope<T>(
    string MessageId,
    string CorrelationId,
    string MessageType,
    DateTime Timestamp,
    int? SequenceNumber,
    T Payload
) where T : class;

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
