using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Application.Common.Security;
using LMS.Application.Features.Materials;
using LMS.Domain.Enums;
using LMS.WebApi.Common;
using LMS.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LMS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class MaterialsController(
    ISender sender,
    ICurrentUserService currentUser,
    IMaterialFileStore fileStore) : ControllerBase
{
    /// <summary>Lists materials visible to the caller.</summary>
    [HttpGet]
    [PermissionAuthorize(Permissions.Materials.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<MaterialDto>>>> List(
        [FromQuery] Guid? classId, CancellationToken ct)
    {
        if (currentUser.UserId is null) return Unauthorized();
        var isAdmin = User.IsInRole(RoleCodes.Admin) || User.IsInRole(RoleCodes.SuperAdmin);
        var r = await sender.Send(new GetMaterialsQuery(currentUser.UserId.Value, isAdmin, classId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<MaterialDto>>.Ok(r.Data, r.Message));
    }

    [HttpGet("{id:guid}")]
    [PermissionAuthorize(Permissions.Materials.Read)]
    public async Task<ActionResult<ApiResponse<MaterialDto>>> GetById(Guid id, CancellationToken ct)
    {
        if (currentUser.UserId is null) return Unauthorized();
        var isAdmin = User.IsInRole(RoleCodes.Admin) || User.IsInRole(RoleCodes.SuperAdmin);
        var r = await sender.Send(new GetMaterialByIdQuery(id, currentUser.UserId.Value, isAdmin), ct);
        if (!r.Success)
        {
            return r.ErrorCode switch
            {
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, ApiResponse<MaterialDto>.Fail(r.Message ?? "Forbidden")),
                "NOT_FOUND" => NotFound(ApiResponse<MaterialDto>.Fail(r.Message ?? "Not found")),
                _ => BadRequest(ApiResponse<MaterialDto>.Fail(r.Message ?? "Failed")),
            };
        }
        return Ok(ApiResponse<MaterialDto>.Ok(r.Data, r.Message));
    }

    /// <summary>
    /// Multipart upload: file + form fields. visibility="Public"|"Private".
    /// classIds is a comma-separated list of class guids required when private.
    /// </summary>
    [HttpPost("upload")]
    [PermissionAuthorize(Permissions.Materials.Manage)]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<ActionResult<ApiResponse<MaterialDto>>> Upload(
        [FromForm] IFormFile file,
        [FromForm] string title,
        [FromForm] string? description,
        [FromForm] string visibility,
        [FromForm] string? classIds,
        CancellationToken ct)
    {
        if (currentUser.UserId is null) return Unauthorized();
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<MaterialDto>.Fail("File is required."));
        if (!Enum.TryParse<MaterialVisibility>(visibility, true, out var parsedVisibility))
            return BadRequest(ApiResponse<MaterialDto>.Fail("visibility must be 'Public' or 'Private'."));

        var parsedClassIds = (classIds ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .Distinct()
            .ToList();

        if (parsedVisibility == MaterialVisibility.Private && parsedClassIds.Count == 0)
            return BadRequest(ApiResponse<MaterialDto>.Fail("Private materials need at least one classId."));

        string storedName;
        try
        {
            await using var stream = file.OpenReadStream();
            storedName = await fileStore.SaveAsync(stream, file.FileName, file.ContentType, ct);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<MaterialDto>.Fail(ex.Message));
        }

        var cmd = new CreateMaterialCommand(
            title, description, parsedVisibility,
            storedName, file.FileName, file.ContentType ?? "application/octet-stream",
            file.Length, currentUser.UserId.Value, parsedClassIds);

        var result = await sender.Send(cmd, ct);
        if (!result.Success)
        {
            // Roll the file back so we don't leave orphans.
            await fileStore.DeleteAsync(storedName, ct);
            return BadRequest(ApiResponse<MaterialDto>.Fail(result.Message ?? "Failed"));
        }
        return Ok(ApiResponse<MaterialDto>.Ok(result.Data, result.Message));
    }

    [HttpPut("{id:guid}")]
    [PermissionAuthorize(Permissions.Materials.Manage)]
    public async Task<ActionResult<ApiResponse<MaterialDto>>> UpdateDetails(Guid id,
        [FromBody] UpdateMaterialDetailsCommand cmd, CancellationToken ct)
    {
        var result = await sender.Send(cmd with { MaterialId = id }, ct);
        return result.Success
            ? Ok(ApiResponse<MaterialDto>.Ok(result.Data, result.Message))
            : BadRequest(ApiResponse<MaterialDto>.Fail(result.Message ?? "Failed"));
    }

    [HttpDelete("{id:guid}")]
    [PermissionAuthorize(Permissions.Materials.Manage)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteMaterialCommand(id), ct);
        if (!result.Success) return BadRequest(ApiResponse<object>.Fail(result.Message ?? "Failed"));
        await fileStore.DeleteAsync(result.Data!, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Deleted"));
    }

    /// <summary>
    /// Streams the material's file. Permission is re-checked through the
    /// query handler — a private material's link can't be shared with a user
    /// who isn't in one of the linked classes.
    /// </summary>
    [HttpGet("{id:guid}/download")]
    [PermissionAuthorize(Permissions.Materials.Read)]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        if (currentUser.UserId is null) return Unauthorized();
        var isAdmin = User.IsInRole(RoleCodes.Admin) || User.IsInRole(RoleCodes.SuperAdmin);
        var lookup = await sender.Send(new GetMaterialByIdQuery(id, currentUser.UserId.Value, isAdmin), ct);
        if (!lookup.Success)
        {
            return lookup.ErrorCode switch
            {
                "FORBIDDEN" => Forbid(),
                "NOT_FOUND" => NotFound(),
                _ => BadRequest(lookup.Message),
            };
        }
        var dto = lookup.Data!;
        // The handler returned us a public DTO; the stored file name lives on
        // the entity, so we re-fetch by id without the auth check (we already
        // passed it). Loading by id from the storage by the dto's id requires
        // another DB roundtrip — acceptable for downloads.
        // For simplicity here we look up the raw entity once again.
        var fileQuery = await sender.Send(new GetMaterialByIdQuery(id, currentUser.UserId.Value, true), ct);
        if (!fileQuery.Success) return NotFound();

        // Pull the stored name via a direct query on the DbContext through
        // the cached handler is not possible from a controller — but we have
        // the DTO carrying OriginalFileName + MimeType. We still need the
        // stored name, so fetch a minimal projection here.
        var storedFileNameLookup = await sender.Send(new GetMaterialStoredFileNameQuery(id), ct);
        if (storedFileNameLookup is null) return NotFound();
        var stream = await fileStore.OpenReadAsync(storedFileNameLookup, ct);
        if (stream is null) return NotFound();

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Content-Disposition"] = $"attachment; filename=\"{dto.OriginalFileName}\"";
        return File(stream, dto.MimeType, dto.OriginalFileName);
    }
}

/// <summary>
/// Tiny side-query used only by the download endpoint to fetch the on-disk
/// file name after permission has already been resolved upstream.
/// </summary>
public sealed record GetMaterialStoredFileNameQuery(Guid MaterialId) : IRequest<string?>;

public sealed class GetMaterialStoredFileNameQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetMaterialStoredFileNameQuery, string?>
{
    public async Task<string?> Handle(GetMaterialStoredFileNameQuery request, CancellationToken ct)
    {
        return await db.Materials.AsNoTracking()
            .Where(m => m.Id == request.MaterialId)
            .Select(m => m.StoredFileName)
            .FirstOrDefaultAsync(ct);
    }
}
