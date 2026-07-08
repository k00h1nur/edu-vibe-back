using LMS.Application.Common.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Common;

/// <summary>
/// Maps a <see cref="Result{T}"/> to the <see cref="ApiResponse{T}"/> envelope +
/// HTTP status the controllers were hand-writing at ~120 call sites. The produced
/// response is byte-for-byte identical to the previous inline ternary:
///   • success → 200 with <c>ApiResponse&lt;T&gt;.Ok(result.Data, result.Message)</c>
///   • failure → 400 (<see cref="ToApiResult{T}"/>) or 404
///     (<see cref="ToApiResultOrNotFound{T}"/>) with
///     <c>ApiResponse&lt;T&gt;.Fail(result.Message ?? fallback)</c>.
///
/// Only the uniform success/BadRequest and success/NotFound shapes are folded in;
/// endpoints that map error codes to bespoke statuses, return an empty body, or
/// surface validation errors keep their explicit inline handling.
/// </summary>
public static class ResultActionExtensions
{
    /// <summary>Success → 200; failure → 400 with message (fallback "Failed").</summary>
    public static ActionResult<ApiResponse<T>> ToApiResult<T>(this Result<T> result) =>
        result.Build(StatusCodes.Status400BadRequest, "Failed");

    /// <summary>Success → 200; failure → 404 with message (fallback "Not found").</summary>
    public static ActionResult<ApiResponse<T>> ToApiResultOrNotFound<T>(this Result<T> result) =>
        result.Build(StatusCodes.Status404NotFound, "Not found");

    private static ActionResult<ApiResponse<T>> Build<T>(this Result<T> result, int failStatus, string failMessage) =>
        result.Success
            ? new OkObjectResult(ApiResponse<T>.Ok(result.Data, result.Message))
            : new ObjectResult(ApiResponse<T>.Fail(result.Message ?? failMessage)) { StatusCode = failStatus };
}
