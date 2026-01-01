namespace Luna.Curriculum.Subjects;

/// <summary>
/// Subject definition: scope, sequence, prerequisites.
/// </summary>
public sealed class Subject
{
    public required string SubjectId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public List<string> UnitIds { get; set; } = new();
}

public sealed class Unit
{
    public required string UnitId { get; init; }
    public required string SubjectId { get; init; }
    public required string Name { get; init; }
    public List<string> LessonIds { get; set; } = new();
    public List<string> PrerequisiteLessonIds { get; set; } = new();
}
