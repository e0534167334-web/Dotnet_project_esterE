namespace Core.Common;

public enum ResultStatus { Success, NotFound, Validation, Conflict, Forbidden, Unauthorized }

public class Result
{
    public ResultStatus Status { get; }
    public string? Message { get; }
    public bool IsSuccess => Status == ResultStatus.Success;

    protected Result(ResultStatus status, string? message)
    {
        Status = status;
        Message = message;
    }

    public static Result Success() => new(ResultStatus.Success, null);
    public static Result Fail(ResultStatus status, string message) => new(status, message);
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(ResultStatus status, string? message, T? value) : base(status, message)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(ResultStatus.Success, null, value);
    public static new Result<T> Fail(ResultStatus status, string message) => new(status, message, default);
}
