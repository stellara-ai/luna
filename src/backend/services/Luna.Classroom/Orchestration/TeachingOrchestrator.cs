// AI CONTRACT:
// Must follow LUNE_CONTEXT.md §2.1 (conversational presence)
// Streaming-first, correlation IDs, timestamps, monotonic sequence

namespace Luna.Classroom.Orchestration;

/// <summary>
/// Teaching orchestration: turn management, strategy selection, flow control.
/// Separates "how the teacher thinks" from "how the teacher is presented".
/// </summary>
public interface ITeachingOrchestrator
{
    Task<TeachingAction> SelectNextActionAsync(TeachingContext context, CancellationToken ct);
}

public static class TeachingActionTypes
{
    public const string Explain = "explain";
    public const string AskQuestion = "question";
    public const string Feedback = "feedback";
}

public sealed class TeachingOrchestrator : ITeachingOrchestrator
{
    public async Task<TeachingAction> SelectNextActionAsync(TeachingContext context, CancellationToken ct)
    {
        // Placeholder orchestration: echo student input as teacher response.
        await Task.Yield();

        return new TeachingAction
        {
            ActionType = TeachingActionTypes.Explain,
            Content = $"I heard you say: '{context.StudentInput}'. Let's keep going on lesson {context.LessonId}.",
            Metadata = new()
            {
                ["sessionId"] = context.SessionId,
                ["studentId"] = context.StudentId
            }
        };
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
    public Dictionary<string, object> StudentMetadata { get; init; } = new();
}

/// <summary>
/// The next action the teacher should execute.
/// </summary>
public sealed class TeachingAction
{
    public string ActionType { get; init; } = TeachingActionTypes.Explain;
    public required string Content { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}
