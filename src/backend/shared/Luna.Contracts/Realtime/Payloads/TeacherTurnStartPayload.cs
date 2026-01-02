namespace Luna.Contracts.Realtime.Payloads;

using Luna.Contracts.Realtime;

/// <summary>
/// Signals the start of a teacher "turn" (a single response stream).
/// Gives the client a stable TurnId + timing origin.
/// </summary>
public sealed class TeacherTurnStartPayload : WsPayload
{
    public required string TurnId { get; init; }

    /// <summary>Always 0 for start.</summary>
    public required int OffsetMs { get; init; }

    /// <summary>Optional: why the turn started (student_input, retry, etc.).</summary>
    public string? Reason { get; init; }
}