using System.Text.Json;
using FluentValidation;

namespace LMS.WebApi.Middleware;

public sealed class GlobalExceptionMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                message = "Validation failed", errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            }));
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { message = ex.Message }));
        }
    }
}