using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.Roles;

public sealed record RolesPingCommand : IRequest<Result<string>>;

public sealed class RolesPingCommandHandler : IRequestHandler<RolesPingCommand, Result<string>>
{
    public Task<Result<string>> Handle(RolesPingCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Ok("Roles module ready"));
    }
}