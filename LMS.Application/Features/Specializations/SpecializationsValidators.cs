using FluentValidation;

namespace LMS.Application.Features.Specializations;

public sealed class CreateSpecializationCommandValidator : AbstractValidator<CreateSpecializationCommand>
{
    public CreateSpecializationCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
    }
}

public sealed class UpdateSpecializationCommandValidator : AbstractValidator<UpdateSpecializationCommand>
{
    public UpdateSpecializationCommandValidator()
    {
        RuleFor(x => x.SpecializationId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
    }
}

public sealed class SetSpecializationActiveCommandValidator : AbstractValidator<SetSpecializationActiveCommand>
{
    public SetSpecializationActiveCommandValidator()
    {
        RuleFor(x => x.SpecializationId).NotEmpty();
    }
}

public sealed class DeleteSpecializationCommandValidator : AbstractValidator<DeleteSpecializationCommand>
{
    public DeleteSpecializationCommandValidator()
    {
        RuleFor(x => x.SpecializationId).NotEmpty();
    }
}

public sealed class SetStaffSpecializationsCommandValidator : AbstractValidator<SetStaffSpecializationsCommand>
{
    public SetStaffSpecializationsCommandValidator()
    {
        RuleFor(x => x.StaffProfileId).NotEmpty();
        RuleFor(x => x.SpecializationIds)
            .NotNull()
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("Duplicate specialization ids are not allowed.");
    }
}
