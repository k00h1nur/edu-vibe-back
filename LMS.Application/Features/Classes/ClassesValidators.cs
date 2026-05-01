using FluentValidation;

namespace LMS.Application.Features.Classes;

public sealed class CreateClassCommandValidator : AbstractValidator<CreateClassCommand>
{
    public CreateClassCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.MaxStudents).GreaterThan(0);
    }
}

public sealed class UpdateClassCommandValidator : AbstractValidator<UpdateClassCommand>
{
    public UpdateClassCommandValidator()
    {
        RuleFor(x => x.ClassId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.MaxStudents).GreaterThan(0);
    }
}

public sealed class IdValidators : AbstractValidator<GetClassByIdQuery>
{
    public IdValidators()
    {
        RuleFor(x => x.ClassId).NotEmpty();
    }
}