namespace Luna.Curriculum.Subjects;

/// <summary>
/// Subject definition: scope, sequence, prerequisites.
/// </summary>
public sealed class Subject
{
    public string SubjectId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public List<string> UnitIds { get; set; } = new();
}

public sealed class Unit
{
    public string UnitId { get; set; }
    public string SubjectId { get; set; }
    public string Name { get; set; }
    public List<string> LessonIds { get; set; } = new();
    public List<string> PrerequisiteLessonIds { get; set; } = new();
}
