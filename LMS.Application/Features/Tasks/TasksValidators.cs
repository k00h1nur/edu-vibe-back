using FluentValidation;

namespace LMS.Application.Features.Tasks;

public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.AssignmentId).NotEmpty();
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Points).GreaterThan(0);
        RuleFor(x => x.ContentJson).NotEmpty().Must(BeValidJson)
            .WithMessage("ContentJson must be valid JSON.");
        RuleFor(x => x.SolutionJson).Must(BeValidJsonOrNull)
            .WithMessage("SolutionJson must be valid JSON when provided.");
    }

    private static bool BeValidJson(string s) => IsValidJson(s);
    private static bool BeValidJsonOrNull(string? s) => s is null || IsValidJson(s);

    private static bool IsValidJson(string s)
    {
        try { System.Text.Json.JsonDocument.Parse(s); return true; }
        catch { return false; }
    }
}

public sealed class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.Order).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Points).GreaterThan(0);
        RuleFor(x => x.ContentJson).NotEmpty();
    }
}

public sealed class ReorderTasksCommandValidator : AbstractValidator<ReorderTasksCommand>
{
    public ReorderTasksCommandValidator()
    {
        RuleFor(x => x.AssignmentId).NotEmpty();
        RuleFor(x => x.TaskIdsInOrder).NotEmpty();
    }
}
