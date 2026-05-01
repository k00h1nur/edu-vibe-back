namespace LMS.Application.Common.Models;

public class Result
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? Message { get; init; }
    public Dictionary<string, string[]> ValidationErrors { get; init; } = new();

    public static Result Ok(string? message = null)
    {
        return new Result { Success = true, Message = message };
    }

    public static Result Fail(string code, string message, Dictionary<string, string[]>? errors = null)
    {
        return new Result
        {
            Success = false, ErrorCode = code, Message = message,
            ValidationErrors = errors ?? new Dictionary<string, string[]>()
        };
    }
}

public class Result<T> : Result
{
    public T? Data { get; init; }

    public static Result<T> Ok(T data, string? message = null)
    {
        return new Result<T> { Success = true, Data = data, Message = message };
    }

    public new static Result<T> Fail(string code, string message, Dictionary<string, string[]>? errors = null)
    {
        return new Result<T>
        {
            Success = false, ErrorCode = code, Message = message,
            ValidationErrors = errors ?? new Dictionary<string, string[]>()
        };
    }
}