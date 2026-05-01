using FluentValidation;

namespace LMS.Application.Features.Attendance;

public sealed class MarkAttendanceCommandValidator : AbstractValidator<MarkAttendanceCommand>
{
    public MarkAttendanceCommandValidator()
    {
        RuleFor(x => x.ClassId).NotEmpty();
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.StudentProfileId).NotEmpty();
    }
}

public sealed class UpdateAttendanceCommandValidator : AbstractValidator<UpdateAttendanceCommand>
{
    public UpdateAttendanceCommandValidator()
    {
        RuleFor(x => x.AttendanceId).NotEmpty();
    }
}