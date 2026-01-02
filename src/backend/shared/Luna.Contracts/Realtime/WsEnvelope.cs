// AI CONTRACT:
// Must follow LUNE_CONTEXT.md §2.1 (conversational presence)
// Streaming-first, correlation IDs, timestamps, monotonic sequence

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
