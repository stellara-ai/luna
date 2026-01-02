namespace Luna.Contracts.Realtime.Payloads;

using Luna.Contracts.Realtime;

/// <summary>
/// Incremental teacher text output (streaming).
/// Designed to align with audio using OffsetMs/DurationMs.
/// </summary>
public sealed class TeacherTextDeltaPayload : WsPayload
{
    /// <summary>Unique id for this teacher turn. Stable across all turn messages.</summary>
    public required string TurnId { get; init; }

    /// <summary>Monotonic delta index within the turn (1-based).</summary>
    public required int DeltaIndex { get; init; }

    /// <summary>
    /// The text delta. Prefer "append-only" deltas for simplicity at first.
    /// </summary>
    public required string Delta { get; init; }

    /// <summary>
    /// Milliseconds since the teacher turn started (shared timing axis).
    /// </summary>
    public required int OffsetMs { get; init; }

    /// <summary>
    /// Optional: approximate duration this delta covers in speech (if known).
    /// Helps lip-sync/highlighting even without audio.
    /// </summary>
    public int? DurationMs { get; init; }

    /// <summary>
    /// True when this is the final text delta for the turn.
    /// </summary>
    public bool IsFinal { get; init; }

    /// <summary>
    /// Optional: if you later support edits, you can set Operation=Replace with a Range.
    /// For v1, keep it append-only and leave null.
    /// </summary>
    public TextDeltaOperation? Operation { get; init; }

    /// <summary>
    /// Optional: character range in the assembled text when Operation is used.
    /// </summary>
    public TextRange? Range { get; init; }
}

public sealed record TextRange(int Start, int Length);