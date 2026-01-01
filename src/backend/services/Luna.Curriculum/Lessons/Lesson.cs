namespace Luna.Curriculum.Lessons;

/// <summary>
/// A lesson: educational content, learning objectives, teaching strategies.
/// </summary>
public sealed class Lesson
{
    public required string LessonId { get; init; }
    public required string Subject { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public int GradeLevel { get; set; }
    public List<LearningObjective> Objectives { get; set; } = new();
    public List<TeachingStrategy> Strategies { get; set; } = new();
    public Dictionary<string, object>? Metadata { get; set; }
}

public sealed class LearningObjective
{
    public required string ObjectiveId { get; init; }
    public required string Description { get; init; }
    public ObjectiveLevel Level { get; set; }
}

public enum ObjectiveLevel { Remember, Understand, Apply, Analyze, Evaluate, Create }

public sealed class TeachingStrategy
{
    public required string StrategyId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public List<string> ApplicableObjectives { get; set; } = new();
    public Dictionary<string, object>? Parameters { get; set; }
}
