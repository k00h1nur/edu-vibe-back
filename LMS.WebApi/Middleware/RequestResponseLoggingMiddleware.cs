namespace LMS.WebApi.Middleware;

public sealed class RequestResponseLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestResponseLoggingMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        logger.LogInformation("Request {Method} {Path}", context.Request.Method, context.Request.Path);
        await next(context);
        logger.LogInformation("Response {StatusCode}", context.Response.StatusCode);
    }
}