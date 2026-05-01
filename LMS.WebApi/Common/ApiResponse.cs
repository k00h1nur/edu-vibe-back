namespace LMS.WebApi.Common;

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }
    public object? Errors { get; init; }

    public static ApiResponse<T> Ok(T? data, string? message = null)
    {
        return new ApiResponse<T> { Success = true, Data = data, Message = message };
    }

    public static ApiResponse<T> Fail(string message, object? errors = null)
    {
        return new ApiResponse<T> { Success = false, Message = message, Errors = errors };
    }
}