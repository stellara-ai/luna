namespace Luna.Classroom.Orchestration;

/// <summary>
/// Teaching orchestration: turn management, strategy selection, flow control.
/// Separates "how the teacher thinks" from "how the teacher is presented".
/// </summary>
public interface ITeachingOrchestrator
{
    Task<TeachingAction> SelectNextActionAsync(TeachingContext context, CancellationToken ct);
}

public sealed class TeachingOrchestrator : ITeachingOrchestrator
{
    public async Task<TeachingAction> SelectNextActionAsync(TeachingContext context, CancellationToken ct)
    {
        // 1. Interpret student input (understanding + attention)
        // 2. Check lesson state
        // 3. Select teaching strategy (from Curriculum)
        // 4. Return action for execution
        await Task.Delay(0, ct);
        throw new NotImplementedException();
    }
}

/// <summary>
/// Context for orchestration decision: student input, lesson state, learner profile.
/// </summary>
public sealed class TeachingContext
{
    public required string StudentId { get; init; }
    public required string LessonId { get; init; }
    public required string SessionId { get; init; }
    public required string StudentInput { get; init; }
    public Dictionary<string, object>? StudentMetadata { get; set; }
}

/// <summary>
/// The next action the teacher should execute.
/// </summary>
public sealed class TeachingAction
{
    public required string ActionType { get; init; } // "explain", "question", "feedback", etc.
    public required string Content { get; init; }
    public Dictionary<string, object>? Metadata { get; set; }
}
