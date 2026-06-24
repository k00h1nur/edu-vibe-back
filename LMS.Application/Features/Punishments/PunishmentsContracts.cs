using LMS.Application.Common.Models;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Punishments;

public sealed record PunishmentDto(
    Guid Id, Guid TeacherId, string Title, string? Description,
    PunishmentType Type, decimal Value, string? Reason,
    Guid AppliedByAdminId, DateOnly PeriodMonth, DateTime CreatedAt);

public sealed record CreatePunishmentCommand(
    Guid TeacherId, string Title, string? Description, PunishmentType Type,
    decimal Value, string? Reason, DateOnly PeriodMonth) : IRequest<Result<PunishmentDto>>;

public sealed record UpdatePunishmentCommand(
    Guid Id, string Title, string? Description, PunishmentType Type,
    decimal Value, string? Reason, DateOnly PeriodMonth) : IRequest<Result<PunishmentDto>>;

public sealed record DeletePunishmentCommand(Guid Id) : IRequest<Result>;

public sealed record GetPunishmentByIdQuery(Guid Id) : IRequest<Result<PunishmentDto>>;

/// <summary>All punishments, optionally filtered by teacher and/or month (month normalised to the 1st).</summary>
public sealed record GetPunishmentsQuery(Guid? TeacherId = null, DateOnly? Month = null)
    : IRequest<Result<IReadOnlyCollection<PunishmentDto>>>;
