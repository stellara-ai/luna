namespace Luna.Classroom.Realtime;

using Luna.Contracts.Realtime;

/// <summary>
/// WebSocket handler for real-time classroom communication.
/// Manages message routing, envelope validation, and lifecycle.
/// </summary>
public interface IWebSocketHandler
{
    Task HandleMessageAsync(string sessionId, string rawMessage, CancellationToken ct);
    Task HandleDisconnectAsync(string sessionId, CancellationToken ct);
}

public sealed class ClassroomWebSocketHandler : IWebSocketHandler
{
    public async Task HandleMessageAsync(string sessionId, string rawMessage, CancellationToken ct)
    {
        // Parse envelope
        // Validate payload type
        // Route to appropriate handler
        // Return response envelope
        await Task.Delay(0, ct);
    }

    public async Task HandleDisconnectAsync(string sessionId, CancellationToken ct)
    {
        // Clean up session
        // Record closure event
        // Notify parent/monitoring
        await Task.Delay(0, ct);
    }
}
