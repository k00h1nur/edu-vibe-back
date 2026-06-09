using LMS.Application.Common.Models;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Materials;

public sealed record MaterialDto(
    Guid Id,
    string Title,
    string? Description,
    MaterialVisibility Visibility,
    string OriginalFileName,
    string MimeType,
    long FileSize,
    Guid UploadedByUserId,
    DateTime CreatedAt,
    IReadOnlyCollection<Guid> ClassIds);

public sealed record CreateMaterialCommand(
    string Title,
    string? Description,
    MaterialVisibility Visibility,
    string StoredFileName,
    string OriginalFileName,
    string MimeType,
    long FileSize,
    Guid UploadedByUserId,
    IReadOnlyList<Guid> ClassIds) : IRequest<Result<MaterialDto>>;

public sealed record UpdateMaterialDetailsCommand(
    Guid MaterialId,
    string Title,
    string? Description,
    MaterialVisibility Visibility,
    IReadOnlyList<Guid> ClassIds) : IRequest<Result<MaterialDto>>;

public sealed record DeleteMaterialCommand(Guid MaterialId) : IRequest<Result<string>>;

public sealed record GetMaterialsQuery(
    Guid CallerUserId,
    bool IsAdmin,
    Guid? ClassId = null) : IRequest<Result<IReadOnlyCollection<MaterialDto>>>;

public sealed record GetMaterialByIdQuery(Guid MaterialId, Guid CallerUserId, bool IsAdmin)
    : IRequest<Result<MaterialDto>>;
