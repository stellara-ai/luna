namespace Luna.Contracts.Realtime.Payloads;

using Luna.Contracts.Realtime;

/// <summary>
/// Signals the end of a teacher turn.
/// Client can stop buffering deltas/audio when it sees this.
/// </summary>
public sealed class TeacherTurnEndPayload : WsPayload
{
    public required string TurnId { get; init; }

    public required int OffsetMs { get; init; }

    /// <summary>completed, cancelled, interrupted, error, etc.</summary>
    public string? Outcome { get; init; }
}