using System.Security.Cryptography;
using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Application.Common.Security;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserRole = LMS.Domain.Entities.UserRole;

namespace LMS.Application.Features.Auth;

public sealed class RegisterUserCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<RegisterUserCommand, Result<AuthTokensResponse>>
{
    public async Task<Result<AuthTokensResponse>> Handle(RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await dbContext.Users.AnyAsync(x => x.Email == email, cancellationToken))
            return Result<AuthTokensResponse>.Fail("EMAIL_EXISTS", "Email already exists.");

        var role = await dbContext.Roles.FirstOrDefaultAsync(x => x.Code == request.RoleCode, cancellationToken);
        if (role is null) return Result<AuthTokensResponse>.Fail("ROLE_NOT_FOUND", "Role does not exist.");

        var user = new User(email, passwordHasher.Hash(request.Password));
        await dbContext.Users.AddAsync(user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.UserRoles.AddAsync(new UserRole(user.Id, role.Id), cancellationToken);

        if (string.Equals(role.Code, RoleCodes.Student, StringComparison.OrdinalIgnoreCase))
        {
            if (!await dbContext.StudentProfiles.AnyAsync(x => x.UserId == user.Id, cancellationToken))
                await dbContext.StudentProfiles.AddAsync(new StudentProfile(user.Id, user), cancellationToken);
        }
        else
        {
            if (!await dbContext.StaffProfiles.AnyAsync(x => x.UserId == user.Id, cancellationToken))
                await dbContext.StaffProfiles.AddAsync(new StaffProfile(user.Id, EmploymentType.FullTime),
                    cancellationToken);
        }

        var roles = new[] { role.Code };
        var permissions = PermissionMatrix.ForRoles(roles);
        var accessToken = jwtTokenGenerator.Generate(user.Id, user.Email, roles, permissions);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var refreshHash = passwordHasher.Hash(refreshToken);

        user.SetRefreshToken(refreshHash, dateTimeProvider.UtcNow.AddDays(30));
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<AuthTokensResponse>.Ok(new AuthTokensResponse(user.Id, user.Email, accessToken, refreshToken,
            roles, permissions));
    }
}

public sealed class LoginCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<LoginCommand, Result<AuthTokensResponse>>
{
    public async Task<Result<AuthTokensResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Email == email, cancellationToken);
        if (user is null) return Result<AuthTokensResponse>.Fail("INVALID_CREDENTIALS", "Invalid credentials.");
        if (user.Status != UserStatus.Active)
            return Result<AuthTokensResponse>.Fail("USER_INACTIVE", "User is not active.");
        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result<AuthTokensResponse>.Fail("INVALID_CREDENTIALS", "Invalid credentials.");

        var roles = await dbContext.UserRoles.Where(x => x.UserId == user.Id)
            .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Code).ToArrayAsync(cancellationToken);
        var permissions = PermissionMatrix.ForRoles(roles);

        var accessToken = jwtTokenGenerator.Generate(user.Id, user.Email, roles, permissions);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var refreshHash = passwordHasher.Hash(refreshToken);
        user.SetRefreshToken(refreshHash, dateTimeProvider.UtcNow.AddDays(30));
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<AuthTokensResponse>.Ok(new AuthTokensResponse(user.Id, user.Email, accessToken, refreshToken,
            roles, permissions));
    }
}

public sealed class RefreshTokenCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<RefreshTokenCommand, Result<AuthTokensResponse>>
{
    public async Task<Result<AuthTokensResponse>> Handle(RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var users = await dbContext.Users.Where(x => x.RefreshTokenHash != null && x.RefreshTokenExpiresAt != null)
            .ToListAsync(cancellationToken);
        var user = users.FirstOrDefault(x =>
            x.RefreshTokenExpiresAt >= dateTimeProvider.UtcNow && x.RefreshTokenHash != null &&
            passwordHasher.Verify(request.RefreshToken, x.RefreshTokenHash));
        if (user is null) return Result<AuthTokensResponse>.Fail("INVALID_REFRESH_TOKEN", "Invalid refresh token.");
        if (user.Status != UserStatus.Active)
            return Result<AuthTokensResponse>.Fail("USER_INACTIVE", "User is not active.");

        var roles = await dbContext.UserRoles.Where(x => x.UserId == user.Id)
            .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Code).ToArrayAsync(cancellationToken);
        var permissions = PermissionMatrix.ForRoles(roles);
        var accessToken = jwtTokenGenerator.Generate(user.Id, user.Email, roles, permissions);

        var newRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        user.SetRefreshToken(passwordHasher.Hash(newRefreshToken), dateTimeProvider.UtcNow.AddDays(30));
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<AuthTokensResponse>.Ok(new AuthTokensResponse(user.Id, user.Email, accessToken, newRefreshToken,
            roles, permissions));
    }
}

public sealed class AssignRoleCommandHandler(IApplicationDbContext dbContext)
    : IRequestHandler<AssignRoleCommand, Result>
{
    public async Task<Result> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
        if (user is null) return Result.Fail("USER_NOT_FOUND", "User not found.");

        var role = await dbContext.Roles.FirstOrDefaultAsync(x => x.Code == request.RoleCode, cancellationToken);
        if (role is null) return Result.Fail("ROLE_NOT_FOUND", "Role not found.");

        var exists =
            await dbContext.UserRoles.AnyAsync(x => x.UserId == user.Id && x.RoleId == role.Id, cancellationToken);
        if (exists) return Result.Ok("Role already assigned.");

        await dbContext.UserRoles.AddAsync(new UserRole(user.Id, role.Id), cancellationToken);

        if (string.Equals(role.Code, RoleCodes.Student, StringComparison.OrdinalIgnoreCase))
        {
            if (!await dbContext.StudentProfiles.AnyAsync(x => x.UserId == user.Id, cancellationToken))
                await dbContext.StudentProfiles.AddAsync(new StudentProfile(user.Id, user), cancellationToken);
        }
        else
        {
            if (!await dbContext.StaffProfiles.AnyAsync(x => x.UserId == user.Id, cancellationToken))
                await dbContext.StaffProfiles.AddAsync(new StaffProfile(user.Id, EmploymentType.FullTime),
                    cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Ok("Role assigned.");
    }
}