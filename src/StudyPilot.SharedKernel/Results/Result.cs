namespace StudyPilot.SharedKernel.Results;

/// <summary>
/// Outcome of an operation that can fail for expected, non-exceptional reasons.
/// Exceptions stay reserved for genuine faults.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess && error is not null)
        {
            throw new ArgumentException("A successful result cannot carry an error.", nameof(error));
        }

        if (!isSuccess && error is null)
        {
            throw new ArgumentException("A failed result requires an error.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error? Error { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, null);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

/// <summary>A <see cref="Result"/> that carries a value when successful.</summary>
public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error? error)
        : base(isSuccess, error) => _value = value;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot read the value of a failed result.");
}
