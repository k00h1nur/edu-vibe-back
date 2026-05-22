using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Users;

public sealed class GetUsersQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetUsersQuery, Result<IReadOnlyCollection<UserDto>>>
{
    public async Task<Result<IReadOnlyCollection<UserDto>>> Handle(GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        var users = await db.Users
            .Select(u => new UserDto(
                u.Id,
                u.Email,
                u.Status,
                db.UserRoles.Where(ur => ur.UserId == u.Id)
                    .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Code).ToList()))
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyCollection<UserDto>>.Ok(users);
    }
}

public sealed class GetUserByIdQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
        if (user is null) return Result<UserDto>.Fail("NOT_FOUND", "User not found.");
        var roles = await db.UserRoles.Where(ur => ur.UserId == user.Id)
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Code).ToListAsync(cancellationToken);
        return Result<UserDto>.Ok(new UserDto(user.Id, user.Email, user.Status, roles));
    }
}

public sealed class CreateUserCommandHandler(IApplicationDbContext db, IPasswordHasher hasher)
    : IRequestHandler<CreateUserCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken))
            return Result<UserDto>.Fail("EMAIL_EXISTS", "Email already exists.");

        var user = new User(email, hasher.Hash(request.Password));
        if (request.Status != user.Status)
        {
            if (request.Status == UserStatus.Active) user.Activate();
            else user.Deactivate();
        }

        await db.Users.AddAsync(user, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result<UserDto>.Ok(new UserDto(user.Id, user.Email, user.Status, Array.Empty<string>()));
    }
}

public sealed class UpdateUserCommandHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateUserCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
        if (user is null) return Result<UserDto>.Fail("NOT_FOUND", "User not found.");

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email && x.Id != request.UserId, cancellationToken))
            return Result<UserDto>.Fail("EMAIL_EXISTS", "Email already exists.");

        typeof(User).GetProperty(nameof(User.Email))!.SetValue(user, email);
        if (request.Status == UserStatus.Active) user.Activate();
        else user.Deactivate();

        await db.SaveChangesAsync(cancellationToken);
        var roles = await db.UserRoles.Where(ur => ur.UserId == user.Id)
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Code).ToListAsync(cancellationToken);
        return Result<UserDto>.Ok(new UserDto(user.Id, user.Email, user.Status, roles));
    }
}

public sealed class DeactivateUserCommandHandler(IApplicationDbContext db)
    : IRequestHandler<DeactivateUserCommand, Result>
{
    public async Task<Result> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
        if (user is null) return Result.Fail("NOT_FOUND", "User not found.");
        user.Deactivate();
        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok("User deactivated.");
    }
}

public sealed class GetMyUserQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetMyUserQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(GetMyUserQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result<UserDto>.Fail("UNAUTHENTICATED", "No authenticated user.");

        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == currentUser.UserId, cancellationToken);
        if (user is null) return Result<UserDto>.Fail("NOT_FOUND", "User not found.");

        var roles = await db.UserRoles.Where(ur => ur.UserId == user.Id)
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Code)
            .ToListAsync(cancellationToken);
        return Result<UserDto>.Ok(new UserDto(user.Id, user.Email, user.Status, roles));
    }
}

public sealed class UpdateMyUserCommandHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpdateMyUserCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(UpdateMyUserCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result<UserDto>.Fail("UNAUTHENTICATED", "No authenticated user.");

        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == currentUser.UserId, cancellationToken);
        if (user is null) return Result<UserDto>.Fail("NOT_FOUND", "User not found.");

        var newEmail = request.Email.Trim().ToLowerInvariant();
        if (newEmail != user.Email &&
            await db.Users.AnyAsync(x => x.Email == newEmail && x.Id != user.Id, cancellationToken))
            return Result<UserDto>.Fail("EMAIL_EXISTS", "Email already in use.");

        typeof(User).GetProperty(nameof(User.Email))!.SetValue(user, newEmail);
        await db.SaveChangesAsync(cancellationToken);

        var roles = await db.UserRoles.Where(ur => ur.UserId == user.Id)
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Code)
            .ToListAsync(cancellationToken);
        return Result<UserDto>.Ok(new UserDto(user.Id, user.Email, user.Status, roles));
    }
}

public sealed class ChangeMyPasswordCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IPasswordHasher hasher)
    : IRequestHandler<ChangeMyPasswordCommand, Result>
{
    public async Task<Result> Handle(ChangeMyPasswordCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail("UNAUTHENTICATED", "No authenticated user.");

        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == currentUser.UserId, cancellationToken);
        if (user is null) return Result.Fail("NOT_FOUND", "User not found.");

        if (!hasher.Verify(request.CurrentPassword, user.PasswordHash))
            return Result.Fail("INVALID_CREDENTIALS", "Current password is incorrect.");

        user.SetPasswordHash(hasher.Hash(request.NewPassword));
        // Invalidate refresh tokens after a password change.
        user.ClearRefreshToken();
        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok("Password updated.");
    }
}