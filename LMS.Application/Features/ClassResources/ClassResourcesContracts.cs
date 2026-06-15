using LMS.Application.Common.Models;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.ClassResources;

/// <summary>
/// A single class-level content item (roadmap / video / link / homework) as the
/// admin, teacher and enrolled students see it.
/// </summary>
public sealed record ClassResourceDto(
    Guid Id,
    Guid ClassId,
    ClassResourceKind Kind,
    string Title,
    string? Url,
    string? Content,
    int Position,
    DateTime CreatedAt);

/// <summary>
/// Read every resource attached to a class, ordered for the hub. Self-scoped in
/// the handler: admin/staff, the class's teacher, or an enrolled student.
/// </summary>
public sealed record GetClassResourcesQuery(Guid ClassId)
    : IRequest<Result<IReadOnlyCollection<ClassResourceDto>>>;

/// <summary>
/// Attach a new class-level resource. Self-scoped: admin/staff or the class's
/// own teacher. <c>ClassId</c> is taken from the route by the controller.
/// </summary>
public sealed record CreateClassResourceCommand(
    Guid ClassId,
    ClassResourceKind Kind,
    string Title,
    string? Url,
    string? Content) : IRequest<Result<ClassResourceDto>>;

/// <summary>Edit an existing class resource (same manage scope as create).</summary>
public sealed record UpdateClassResourceCommand(
    Guid ClassId,
    Guid ResourceId,
    ClassResourceKind Kind,
    string Title,
    string? Url,
    string? Content) : IRequest<Result<ClassResourceDto>>;

/// <summary>Remove a class resource (same manage scope as create).</summary>
public sealed record DeleteClassResourceCommand(Guid ClassId, Guid ResourceId) : IRequest<Result>;
