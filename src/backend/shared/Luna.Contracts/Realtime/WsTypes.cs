namespace Luna.Contracts.Realtime;

/// <summary>
/// Classroom-specific WebSocket message types.
/// Versioned and stable — treated as public API.
/// </summary>
public static class WsTypes
{
    // ------------------------------
    // Session lifecycle
    // ------------------------------
    public const string SessionStart = "v1.classroom.session_start";
    public const string SessionEnd   = "v1.classroom.session_end";
    public const string SessionEvent = "v1.classroom.session_event";

    // ------------------------------
    // Student → Teacher
    // ------------------------------
    public const string StudentInput  = "v1.classroom.student_input";
    public const string ControlSignal = "v1.classroom.control_signal";

    // ------------------------------
    // Teacher turn orchestration
    // ------------------------------
    public const string TeacherTurnStart = "v1.classroom.teacher_turn_start";
    public const string TeacherTurnEnd   = "v1.classroom.teacher_turn_end";

    // ------------------------------
    // Teacher output (streamed)
    // ------------------------------
    public const string TeacherResponse     = "v1.classroom.teacher_response";      // legacy / aggregate
    public const string TeacherTextDelta    = "v1.classroom.teacher_text_delta";     // incremental text
    public const string TeacherAudioChunk   = "v1.classroom.teacher_audio_chunk";    // base64 or binary
    public const string TeacherMark         = "v1.classroom.teacher_mark";           // timing markers
    public const string TeacherExpression   = "v1.classroom.teacher_expression";     // emotion / gesture
    public const string TeacherViseme       = "v1.classroom.teacher_viseme";          // lip-sync (optional, later)

    // ------------------------------
    // System / transport
    // ------------------------------
    public const string Error = "v1.classroom.error";
    public const string Ping  = "v1.classroom.ping";
    public const string Pong  = "v1.classroom.pong";

    // Student → Server audio streaming (speech-to-text pipeline)
    //
    // These messages allow the client to stream microphone audio in real time
    // using small PCM chunks over WebSocket. They are turn-scoped via TurnId
    // and are designed for low-latency, conversational speech input.
    //
    // Flow:
    //   - StudentAudioStart : begins an audio turn (allocates buffer / STT session)
    //   - StudentAudioChunk : carries raw audio frames (PCM)
    //   - StudentAudioEnd   : signals end-of-speech for the turn
    //
    // STT processing may occur incrementally or after AudioEnd, depending on model.
    public const string StudentAudioStart = "v1.classroom.student_audio_start";
    public const string StudentAudioChunk = "v1.classroom.student_audio_chunk";
    public const string StudentAudioEnd   = "v1.classroom.student_audio_end";
}

/// <summary>
/// Control signals the student can send during a lesson.
/// Kept here because it is protocol-level, not UI-level.
/// </summary>
public enum ControlSignal
{
    Repeat,
    Slower,
    Faster,
    Confused,
    Understood,
    Skip
}