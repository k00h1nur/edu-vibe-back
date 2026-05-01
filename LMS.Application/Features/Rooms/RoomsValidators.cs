using FluentValidation;

namespace LMS.Application.Features.Rooms;

public sealed class CreateRoomCommandValidator : AbstractValidator<CreateRoomCommand>
{
    public CreateRoomCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Capacity).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateRoomCommandValidator : AbstractValidator<UpdateRoomCommand>
{
    public UpdateRoomCommandValidator()
    {
        RuleFor(x => x.RoomId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Capacity).GreaterThanOrEqualTo(0);
    }
}

public sealed class DeleteRoomCommandValidator : AbstractValidator<DeleteRoomCommand>
{
    public DeleteRoomCommandValidator()
    {
        RuleFor(x => x.RoomId).NotEmpty();
    }
}