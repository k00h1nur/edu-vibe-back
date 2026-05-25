using FluentValidation;

namespace LMS.Application.Features.VisitorMessages;

public sealed class CreateVisitorMessageCommandValidator : AbstractValidator<CreateVisitorMessageCommand>
{
    public CreateVisitorMessageCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Phone)
            .NotEmpty().MaximumLength(64)
            .Matches(@"^[+0-9()\-\s]{5,}$")
            .WithMessage("Phone must contain only digits and the usual + ( ) - separators.");
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Message).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.Course).MaximumLength(128);
        RuleFor(x => x.PreferredTime).MaximumLength(128);
        RuleFor(x => x.Language).MaximumLength(8);
    }
}

public sealed class GetVisitorMessagesQueryValidator : AbstractValidator<GetVisitorMessagesQuery>
{
    public GetVisitorMessagesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class MarkVisitorMessageReadCommandValidator : AbstractValidator<MarkVisitorMessageReadCommand>
{
    public MarkVisitorMessageReadCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
