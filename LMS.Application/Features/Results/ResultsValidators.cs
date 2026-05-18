using FluentValidation;

namespace LMS.Application.Features.Results;

public sealed class CreateResultCommandValidator : AbstractValidator<CreateResultCommand>
{
    public CreateResultCommandValidator()
    {
        RuleFor(x => x.StudentFullName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.OverallScore).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Language).NotEmpty().MaximumLength(10);
    }
}

public sealed class UpdateResultCommandValidator : AbstractValidator<UpdateResultCommand>
{
    public UpdateResultCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.StudentFullName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.OverallScore).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Language).NotEmpty().MaximumLength(10);
    }
}
