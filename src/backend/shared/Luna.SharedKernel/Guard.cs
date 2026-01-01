namespace Luna.SharedKernel;

/// <summary>
/// Guard clauses for precondition validation.
/// Throws ArgumentException/InvalidOperationException with clear messages.
/// </summary>
public static class Guard
{
    public static void Against<T>(bool condition, string message) where T : Exception
    {
        if (condition)
        {
            throw (T)Activator.CreateInstance(typeof(T), message)!;
        }
    }

    public static void IsNotNull(object? value, string paramName)
    {
        if (value is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }

    public static void IsNotNullOrEmpty(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{paramName} cannot be null or empty.", paramName);
        }
    }

    public static void IsNotEmpty<T>(IEnumerable<T>? collection, string paramName)
    {
        if (collection?.Any() != true)
        {
            throw new ArgumentException($"{paramName} cannot be empty.", paramName);
        }
    }

    public static void IsInRange(int value, int min, int max, string paramName)
    {
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(paramName, $"{paramName} must be between {min} and {max}.");
        }
    }

    public static void IsGreaterThan(int value, int threshold, string paramName)
    {
        if (value <= threshold)
        {
            throw new ArgumentOutOfRangeException(paramName, $"{paramName} must be greater than {threshold}.");
        }
    }
}
