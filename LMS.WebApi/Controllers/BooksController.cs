using LMS.Application.Common.Models;
using LMS.Application.Common.Security;
using LMS.Application.Features.Books;
using LMS.WebApi.Common;
using LMS.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class BooksController(ISender sender) : ControllerBase
{
    [HttpGet]
    [PermissionAuthorize(Permissions.Books.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<BookDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] string? subject = null,
        [FromQuery] string? level = null,
        CancellationToken ct = default)
    {
        var r = await sender.Send(new GetBooksQuery(page, pageSize, search, subject, level), ct);
        return Ok(ApiResponse<PagedResult<BookDto>>.Ok(r.Data, r.Message));
    }

    [HttpGet("{id:guid}")]
    [PermissionAuthorize(Permissions.Books.Read)]
    public async Task<ActionResult<ApiResponse<BookDto>>> Get(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new GetBookByIdQuery(id), ct);
        return r.Success
            ? Ok(ApiResponse<BookDto>.Ok(r.Data, r.Message))
            : NotFound(ApiResponse<BookDto>.Fail(r.Message ?? "Not found"));
    }

    [HttpPost]
    [PermissionAuthorize(Permissions.Books.Manage)]
    public async Task<ActionResult<ApiResponse<BookDto>>> Create([FromBody] CreateBookCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<BookDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<BookDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPut("{id:guid}")]
    [PermissionAuthorize(Permissions.Books.Manage)]
    public async Task<ActionResult<ApiResponse<BookDto>>> Update(Guid id,
        [FromBody] UpdateBookCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd with { BookId = id }, ct);
        return r.Success
            ? Ok(ApiResponse<BookDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<BookDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpDelete("{id:guid}")]
    [PermissionAuthorize(Permissions.Books.Manage)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new DeleteBookCommand(id), ct);
        return r.Success
            ? Ok(ApiResponse<object>.Ok(new { }, r.Message))
            : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }
}
