namespace Luna.Contracts.Realtime.Payloads;

using Luna.Contracts.Realtime;

/// <summary>
/// Streaming teacher audio. Supports base64 (easy) or binary frames (later).
/// OffsetMs aligns audio start to the teacher turn timeline.
/// </summary>
public sealed class TeacherAudioChunkPayload : WsPayload
{
    public required string TurnId { get; init; }

    /// <summary>Monotonic chunk index within the turn (1-based).</summary>
    public required int ChunkIndex { get; init; }

    /// <summary>Milliseconds since the teacher turn started when this chunk begins.</summary>
    public required int OffsetMs { get; init; }

    /// <summary>Optional: duration represented by this audio chunk.</summary>
    public int? DurationMs { get; init; }

    /// <summary>
    /// Audio format descriptor so clients can decode without guessing.
    /// Example: "audio/pcm;rate=24000;bits=16;channels=1"
    /// or "audio/opus;rate=48000;channels=1"
    /// </summary>
    public required string Format { get; init; }

    /// <summary>
    /// Base64 audio bytes (good for quick v1).
    /// If you later switch to binary websocket frames, this can be null.
    /// </summary>
    public string? AudioBase64 { get; init; }

    /// <summary>
    /// True when this is the final chunk for the turn.
    /// </summary>
    public bool IsFinal { get; init; }
}