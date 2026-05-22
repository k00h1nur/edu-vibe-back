using LMS.Application.Common.Models;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Attendance;

public sealed record AttendanceDto(
    Guid Id,
    Guid ClassId,
    Guid SessionId,
    Guid StudentProfileId,
    AttendanceStatus Status);

public sealed record AttendancePingCommand : IRequest<Result<string>>;

public sealed class AttendancePingCommandHandler : IRequestHandler<AttendancePingCommand, Result<string>>
{
    public Task<Result<string>> Handle(AttendancePingCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Ok("Attendance module ready"));
    }
}

public sealed record MarkAttendanceCommand(Guid ClassId, Guid SessionId, Guid StudentProfileId, AttendanceStatus Status)
    : IRequest<Result<AttendanceDto>>;

public sealed record UpdateAttendanceCommand(Guid AttendanceId, AttendanceStatus Status)
    : IRequest<Result<AttendanceDto>>;

public sealed record GetSessionAttendanceQuery(Guid SessionId) : IRequest<Result<IReadOnlyCollection<AttendanceDto>>>;

public sealed record GetStudentAttendanceQuery(Guid StudentProfileId)
    : IRequest<Result<IReadOnlyCollection<AttendanceDto>>>;

/// <summary>Lists attendance records with optional filters; used by admin overview screens.</summary>
public sealed record GetAttendanceQuery(
    Guid? ClassId = null,
    Guid? SessionId = null,
    Guid? StudentProfileId = null,
    AttendanceStatus? Status = null)
    : IRequest<Result<IReadOnlyCollection<AttendanceDto>>>;