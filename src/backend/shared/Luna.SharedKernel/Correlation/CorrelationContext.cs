namespace Luna.SharedKernel.Correlation;

/// <summary>
/// Correlation ID context for distributed tracing.
/// Used to track related operations across module boundaries.
/// </summary>
public sealed class CorrelationContext
{
    public string CorrelationId { get; }
    public string UserId { get; }
    public DateTime CreatedAt { get; }

    public CorrelationContext(string correlationId, string userId)
    {
        Guard.IsNotNullOrEmpty(correlationId, nameof(correlationId));
        Guard.IsNotNullOrEmpty(userId, nameof(userId));

        CorrelationId = correlationId;
        UserId = userId;
        CreatedAt = DateTime.UtcNow;
    }

    public static CorrelationContext Create(string userId) =>
        new(Guid.NewGuid().ToString("N"), userId);
}
