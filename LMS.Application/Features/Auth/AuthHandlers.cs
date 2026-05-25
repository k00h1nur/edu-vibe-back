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

        StudentProfile? studentProfile = null;
        StaffProfile? staffProfile = null;

        if (string.Equals(role.Code, RoleCodes.Student, StringComparison.OrdinalIgnoreCase))
        {
            studentProfile = await dbContext.StudentProfiles
                .FirstOrDefaultAsync(x => x.UserId == user.Id, cancellationToken);
            if (studentProfile is null)
            {
                studentProfile = new StudentProfile(user.Id, user);
                await dbContext.StudentProfiles.AddAsync(studentProfile, cancellationToken);
            }
        }
        else
        {
            staffProfile = await dbContext.StaffProfiles
                .FirstOrDefaultAsync(x => x.UserId == user.Id, cancellationToken);
            if (staffProfile is null)
            {
                staffProfile = new StaffProfile(user.Id, EmploymentType.FullTime);
                await dbContext.StaffProfiles.AddAsync(staffProfile, cancellationToken);
            }
        }

        var roles = new[] { role.Code };
        var permissions = await dbContext.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .Join(dbContext.Permissions, rp => rp.PermissionId, p => p.Id, (rp, p) => p.Code)
            .ToArrayAsync(cancellationToken);
        var accessToken = jwtTokenGenerator.Generate(
            user.Id, user.Email, roles, permissions,
            studentProfileId: studentProfile?.Id,
            staffProfileId: staffProfile?.Id);
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
        var permissions = await dbContext.UserRoles.Where(x => x.UserId == user.Id)
            .Join(dbContext.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp.PermissionId)
            .Join(dbContext.Permissions, pid => pid, p => p.Id, (pid, p) => p.Code)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var studentProfileId = await dbContext.StudentProfiles
            .Where(x => x.UserId == user.Id)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var staffProfileId = await dbContext.StaffProfiles
            .Where(x => x.UserId == user.Id)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var accessToken = jwtTokenGenerator.Generate(
            user.Id, user.Email, roles, permissions,
            studentProfileId: studentProfileId,
            staffProfileId: staffProfileId);
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
        var permissions = await dbContext.UserRoles.Where(x => x.UserId == user.Id)
            .Join(dbContext.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp.PermissionId)
            .Join(dbContext.Permissions, pid => pid, p => p.Id, (pid, p) => p.Code)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var studentProfileId = await dbContext.StudentProfiles
            .Where(x => x.UserId == user.Id)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var staffProfileId = await dbContext.StaffProfiles
            .Where(x => x.UserId == user.Id)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var accessToken = jwtTokenGenerator.Generate(
            user.Id, user.Email, roles, permissions,
            studentProfileId: studentProfileId,
            staffProfileId: staffProfileId);

        var newRefreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        user.SetRefreshToken(passwordHasher.Hash(newRefreshToken), dateTimeProvider.UtcNow.AddDays(30));
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<AuthTokensResponse>.Ok(new AuthTokensResponse(user.Id, user.Email, accessToken, newRefreshToken,
            roles, permissions));
    }
}

public sealed class AssignRoleCommandHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser)
    : IRequestHandler<AssignRoleCommand, Result>
{
    public async Task<Result> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        // Anti-privilege-escalation guards. The controller already gates on
        // Auth.AssignRole permission; these are defence-in-depth.
        if (currentUser.UserId is null)
            return Result.Fail("UNAUTHENTICATED", "Caller must be authenticated.");

        if (request.UserId == currentUser.UserId.Value)
            return Result.Fail("SELF_ROLE_CHANGE_FORBIDDEN", "You cannot modify your own roles.");

        // Only a SuperAdmin can promote another user to SuperAdmin.
        if (string.Equals(request.RoleCode, RoleCodes.SuperAdmin, StringComparison.OrdinalIgnoreCase)
            && !currentUser.IsInRole(RoleCodes.SuperAdmin))
            return Result.Fail("INSUFFICIENT_PRIVILEGES",
                "Only a SuperAdmin may grant the SuperAdmin role.");

        // Only Admin/SuperAdmin can grant Admin.
        if (string.Equals(request.RoleCode, RoleCodes.Admin, StringComparison.OrdinalIgnoreCase)
            && !currentUser.IsInRole(RoleCodes.SuperAdmin)
            && !currentUser.IsInRole(RoleCodes.Admin))
            return Result.Fail("INSUFFICIENT_PRIVILEGES",
                "Only an Admin or SuperAdmin may grant the Admin role.");

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
