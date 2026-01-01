namespace Luna.Contracts.Realtime;

/// <summary>
/// Classroom-specific WebSocket message types.
/// </summary>
public static class WsTypes
{
    public const string StudentInput = "classroom.student_input";
    public const string TeacherResponse = "classroom.teacher_response";
    public const string TeacherTurnStart = "classroom.teacher_turn_start";
    public const string TeacherTurnEnd = "classroom.teacher_turn_end";
    public const string SessionStart = "classroom.session_start";
    public const string SessionEnd = "classroom.session_end";
    public const string ControlSignal = "classroom.control_signal";
    public const string SessionEvent = "classroom.session_event";
}

/// <summary>
/// Control signals the student can send during a lesson.
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
