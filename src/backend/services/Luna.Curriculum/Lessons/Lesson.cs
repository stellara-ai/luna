namespace Luna.Curriculum.Lessons;

/// <summary>
/// A lesson: educational content, learning objectives, teaching strategies.
/// </summary>
public sealed class Lesson
{
    public string LessonId { get; set; }
    public string Subject { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public int GradeLevel { get; set; }
    public List<LearningObjective> Objectives { get; set; } = new();
    public List<TeachingStrategy> Strategies { get; set; } = new();
    public Dictionary<string, object>? Metadata { get; set; }
}

public sealed class LearningObjective
{
    public string ObjectiveId { get; set; }
    public string Description { get; set; }
    public ObjectiveLevel Level { get; set; }
}

public enum ObjectiveLevel { Remember, Understand, Apply, Analyze, Evaluate, Create }

public sealed class TeachingStrategy
{
    public string StrategyId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> ApplicableObjectives { get; set; } = new();
    public Dictionary<string, object>? Parameters { get; set; }
}
