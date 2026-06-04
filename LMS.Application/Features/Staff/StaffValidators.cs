using FluentValidation;

namespace LMS.Application.Features.Staff;

public sealed class CreateStaffCommandValidator : AbstractValidator<CreateStaffCommand>
{
    public CreateStaffCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}

public sealed class UpdateStaffProfileCommandValidator : AbstractValidator<UpdateStaffProfileCommand>
{
    public UpdateStaffProfileCommandValidator()
    {
        RuleFor(x => x.StaffProfileId).NotEmpty();
    }
}

public sealed class UpdateStaffDetailsCommandValidator : AbstractValidator<UpdateStaffDetailsCommand>
{
    public UpdateStaffDetailsCommandValidator()
    {
        RuleFor(x => x.StaffProfileId).NotEmpty();
        // Domain layer enforces max lengths and trims input. Defense-in-depth
        // upper bounds here mirror what UpdateStudentDetailsCommandValidator does.
        RuleFor(x => x.FirstName).MaximumLength(128);
        RuleFor(x => x.LastName).MaximumLength(128);
        RuleFor(x => x.PhoneNumber).MaximumLength(32);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}