namespace Luna.Students.Profiles;

/// <summary>
/// Student learning profile: preferences, accommodations, attention patterns.
/// </summary>
public sealed class StudentProfile
{
    public string StudentId { get; set; }
    public string Name { get; set; }
    public StudentLearningPreferences Preferences { get; set; } = new();
    public ADHDAccommodations? ADHDAccommodations { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class StudentLearningPreferences
{
    public PaceLevel PreferredPace { get; set; } = PaceLevel.Normal;
    public ExplanationStyle PreferredExplanationStyle { get; set; } = ExplanationStyle.Detailed;
    public bool PreferAudio { get; set; } = true;
    public int MaxSessionDurationMinutes { get; set; } = 30;
}

public enum PaceLevel { Slow, Normal, Fast }
public enum ExplanationStyle { Simple, Detailed, Interactive }

public sealed class ADHDAccommodations
{
    public int BreakFrequencyMinutes { get; set; } = 15;
    public bool EnableRepeatButton { get; set; } = true;
    public bool HighContrastUI { get; set; } = false;
    public int MinimalDistractionMode { get; set; } = 1; // 0=off, 1=on
    public Dictionary<string, object>? CustomAccommodations { get; set; }
}
