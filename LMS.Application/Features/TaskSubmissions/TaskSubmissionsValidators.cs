using FluentValidation;

namespace LMS.Application.Features.TaskSubmissions;

public sealed class SubmitTaskResponseCommandValidator : AbstractValidator<SubmitTaskResponseCommand>
{
    public SubmitTaskResponseCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.StudentProfileId).NotEmpty();
        RuleFor(x => x.ResponseJson).NotEmpty().Must(BeValidJson)
            .WithMessage("ResponseJson must be valid JSON.");
    }

    private static bool BeValidJson(string s)
    {
        try { System.Text.Json.JsonDocument.Parse(s); return true; }
        catch { return false; }
    }
}

public sealed class GradeTaskSubmissionCommandValidator : AbstractValidator<GradeTaskSubmissionCommand>
{
    public GradeTaskSubmissionCommandValidator()
    {
        RuleFor(x => x.SubmissionId).NotEmpty();
        RuleFor(x => x.Score).InclusiveBetween(0m, 1m);
        RuleFor(x => x.Feedback).MaximumLength(2000);
    }
}
