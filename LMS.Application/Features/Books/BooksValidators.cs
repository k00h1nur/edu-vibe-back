using FluentValidation;

namespace LMS.Application.Features.Books;

public sealed class CreateBookCommandValidator : AbstractValidator<CreateBookCommand>
{
    public CreateBookCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Author).MaximumLength(256);
        RuleFor(x => x.Subject).MaximumLength(64);
        RuleFor(x => x.Level).MaximumLength(32);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.CoverImageUrl).MaximumLength(1024);
        RuleFor(x => x.FileUrl).MaximumLength(1024);
    }
}

public sealed class UpdateBookCommandValidator : AbstractValidator<UpdateBookCommand>
{
    public UpdateBookCommandValidator()
    {
        RuleFor(x => x.BookId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Author).MaximumLength(256);
        RuleFor(x => x.Subject).MaximumLength(64);
        RuleFor(x => x.Level).MaximumLength(32);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.CoverImageUrl).MaximumLength(1024);
        RuleFor(x => x.FileUrl).MaximumLength(1024);
    }
}

public sealed class GetBooksQueryValidator : AbstractValidator<GetBooksQuery>
{
    public GetBooksQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
