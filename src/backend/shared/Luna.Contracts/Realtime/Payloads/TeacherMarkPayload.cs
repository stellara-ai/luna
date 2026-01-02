namespace Luna.Contracts.Realtime.Payloads;

using Luna.Contracts.Realtime;

/// <summary>
/// Timed marker emitted during teacher output.
/// Used for text highlighting, pauses, emphasis, gesture sync, etc.
/// </summary>
public sealed class TeacherMarkPayload : WsPayload
{
    public required string TurnId { get; init; }

    /// <summary>Monotonic mark index within the turn (1-based).</summary>
    public required int MarkIndex { get; init; }

    /// <summary>Milliseconds since the teacher turn started.</summary>
    public required int OffsetMs { get; init; }

    /// <summary>Mark type (pause, emphasis, highlight, sentence boundary, etc.).</summary>
    public required string Kind { get; init; }

    /// <summary>
    /// Optional: additional mark payload.
    /// Examples:
    /// - { "charStart": 12, "charLength": 7 } for text highlight
    /// - { "strength": "medium" } for emphasis
    /// - { "name": "beat" } for audio beat marker
    /// </summary>
    public Dictionary<string, object> Data { get; init; } = new();
}