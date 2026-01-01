namespace Luna.SharedKernel.Time;

/// <summary>
/// Abstraction for system clock to enable testing and precision timing.
/// </summary>
public interface ISystemClock
{
    DateTime Now { get; }
    DateTime UtcNow { get; }
}

/// <summary>
/// Default implementation using system clock.
/// </summary>
public sealed class SystemClock : ISystemClock
{
    public DateTime Now => DateTime.Now;
    public DateTime UtcNow => DateTime.UtcNow;
}

/// <summary>
/// Test implementation with fixed clock time.
/// </summary>
public sealed class TestClock : ISystemClock
{
    private DateTime _currentTime = DateTime.UtcNow;

    public DateTime Now => _currentTime;
    public DateTime UtcNow => _currentTime;

    public void SetTime(DateTime time) => _currentTime = time;
    public void Advance(TimeSpan timeSpan) => _currentTime = _currentTime.Add(timeSpan);
}
