using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.Auth;

public sealed record AuthPingCommand : IRequest<Result<string>>;

public sealed class AuthPingCommandHandler : IRequestHandler<AuthPingCommand, Result<string>>
{
    public Task<Result<string>> Handle(AuthPingCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Ok("Auth module ready"));
    }
}

public sealed record AuthTokensResponse(
    Guid UserId,
    string Email,
    string AccessToken,
    string RefreshToken,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);

public sealed record RegisterUserCommand(string Email, string Password, string RoleCode)
    : IRequest<Result<AuthTokensResponse>>;

public sealed record LoginCommand(string Email, string Password) : IRequest<Result<AuthTokensResponse>>;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthTokensResponse>>;

public sealed record AssignRoleCommand(Guid UserId, string RoleCode) : IRequest<Result>;