using FluentValidation;
using LMS.Domain.Enums;

namespace LMS.Application.Features.Attendance;

public sealed class MarkAttendanceCommandValidator : AbstractValidator<MarkAttendanceCommand>
{
    public MarkAttendanceCommandValidator()
    {
        RuleFor(x => x.ClassId).NotEmpty();
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.StudentProfileId).NotEmpty();
        // Bind to a real enum member — guards against a malicious / buggy client
        // posting a numeric value outside the defined range.
        RuleFor(x => x.Status).IsInEnum()
            .WithMessage("Status must be one of: Present, Absent, Late, Excused.");
    }
}

public sealed class UpdateAttendanceCommandValidator : AbstractValidator<UpdateAttendanceCommand>
{
    public UpdateAttendanceCommandValidator()
    {
        RuleFor(x => x.AttendanceId).NotEmpty();
        RuleFor(x => x.Status).IsInEnum()
            .WithMessage("Status must be one of: Present, Absent, Late, Excused.");
    }
}
