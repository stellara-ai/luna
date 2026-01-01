namespace Luna.SharedKernel;

/// <summary>
/// Railway-oriented Result<T> type for explicit error handling.
/// Avoids exceptions for control flow; enables composable error handling.
/// </summary>
public abstract record Result<T>
{
    public sealed record Success(T Value) : Result<T>;
    public sealed record Failure(string Message, Exception? Exception = null) : Result<T>;

    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<string, Exception?, TResult> onFailure) =>
        this switch
        {
            Success success => onSuccess(success.Value),
            Failure failure => onFailure(failure.Message, failure.Exception),
            _ => throw new InvalidOperationException("Unknown Result type")
        };

    public void Match(
        Action<T> onSuccess,
        Action<string, Exception?> onFailure)
    {
        switch (this)
        {
            case Success success:
                onSuccess(success.Value);
                break;
            case Failure failure:
                onFailure(failure.Message, failure.Exception);
                break;
        }
    }

    public static implicit operator Result<T>(T value) => new Success(value);
    public static implicit operator Result<T>(string error) => new Failure(error);
}

/// <summary>
/// Non-generic Result for operations with no return value.
/// </summary>
public abstract record Result
{
    public sealed record Success : Result;
    public sealed record Failure(string Message, Exception? Exception = null) : Result;

    public TResult Match<TResult>(
        Func<TResult> onSuccess,
        Func<string, Exception?, TResult> onFailure) =>
        this switch
        {
            Success => onSuccess(),
            Failure failure => onFailure(failure.Message, failure.Exception),
            _ => throw new InvalidOperationException("Unknown Result type")
        };

    public void Match(
        Action onSuccess,
        Action<string, Exception?> onFailure)
    {
        switch (this)
        {
            case Success:
                onSuccess();
                break;
            case Failure failure:
                onFailure(failure.Message, failure.Exception);
                break;
        }
    }

    public static implicit operator Result(Unit _) => new Success();
    public static implicit operator Result(string error) => new Failure(error);
}

/// <summary>
/// Unit type for void operations.
/// </summary>
public readonly struct Unit;
