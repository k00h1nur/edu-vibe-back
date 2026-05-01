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