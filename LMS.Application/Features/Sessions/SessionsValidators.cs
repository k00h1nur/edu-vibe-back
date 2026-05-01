using FluentValidation;

namespace LMS.Application.Features.Sessions;

public sealed class CreateClassSessionCommandValidator : AbstractValidator<CreateClassSessionCommand>
{
    public CreateClassSessionCommandValidator()
    {
        RuleFor(x => x.ClassId).NotEmpty();
        RuleFor(x => x.StartsAt).LessThan(x => x.EndsAt);
    }
}

public sealed class UpdateClassSessionCommandValidator : AbstractValidator<UpdateClassSessionCommand>
{
    public UpdateClassSessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.StartsAt).LessThan(x => x.EndsAt);
    }
}