namespace SharedKernel;

public class Result
{
    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public ResultError? Error { get; }

    protected Result(bool isSuccess, ResultError? error)
    {
        if (isSuccess && error is not null)
        {
            throw new InvalidOperationException("A successful result cannot carry an error.");
        }

        if (!isSuccess && error is null)
        {
            throw new InvalidOperationException("A failed result must carry an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);

    public static Result Failure(ResultError error) => new(false, error);

    public static Result Invalid(string code, string message) =>
        Failure(new ResultError(ResultType.Invalid, code, message));

    public static Result NotFound(string code, string message) =>
        Failure(new ResultError(ResultType.NotFound, code, message));

    public static Result Conflicted(string code, string message) =>
        Failure(new ResultError(ResultType.Conflicted, code, message));

    public static Result Forbidden(string code, string message) =>
        Failure(new ResultError(ResultType.Forbidden, code, message));

    public static Result Unauthorized(string code, string message) =>
        Failure(new ResultError(ResultType.Unauthorized, code, message));

    public static Result InternalError(string code, string message) =>
        Failure(new ResultError(ResultType.InternalError, code, message));
}

public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(T value)
        : base(true, null)
    {
        Value = value;
    }

    private Result(ResultError error)
        : base(false, error)
    {
    }

    public static Result<T> Success(T value) => new(value);

    public new static Result<T> Failure(ResultError error) => new(error);

    public new static Result<T> Invalid(string code, string message) =>
        Failure(new ResultError(ResultType.Invalid, code, message));

    public new static Result<T> NotFound(string code, string message) =>
        Failure(new ResultError(ResultType.NotFound, code, message));

    public new static Result<T> Conflicted(string code, string message) =>
        Failure(new ResultError(ResultType.Conflicted, code, message));

    public new static Result<T> Forbidden(string code, string message) =>
        Failure(new ResultError(ResultType.Forbidden, code, message));

    public new static Result<T> Unauthorized(string code, string message) =>
        Failure(new ResultError(ResultType.Unauthorized, code, message));

    public new static Result<T> InternalError(string code, string message) =>
        Failure(new ResultError(ResultType.InternalError, code, message));
}
