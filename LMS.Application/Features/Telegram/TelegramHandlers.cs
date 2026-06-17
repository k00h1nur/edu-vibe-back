using System.Security.Cryptography;
using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Application.Common.Security;
using LMS.Application.Features.Auth;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserRole = LMS.Domain.Entities.UserRole;

namespace LMS.Application.Features.Telegram;

/// <summary>
/// Telegram Mini App sign-in. Verifies the signed initData, then resolves it to
/// a platform session:
///   • known Telegram id → sign the linked user in (refreshing the cached
///     Telegram profile);
///   • new Telegram id   → provision a Student account (synthetic email, random
///     password the user never uses — they always re-enter via Telegram) and
///     link it.
/// Either way it returns the exact <see cref="AuthTokensResponse"/> the
/// email/password login emits, so the rest of the stack is auth-source agnostic.
/// </summary>
public sealed class TelegramAuthCommandHandler(
    IApplicationDbContext dbContext,
    ITelegramInitDataValidator validator,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<TelegramAuthCommand, Result<AuthTokensResponse>>
{
    public async Task<Result<AuthTokensResponse>> Handle(TelegramAuthCommand request,
        CancellationToken cancellationToken)
    {
        var (tg, error) = validator.Validate(request.InitData);
        if (tg is null)
            return Result<AuthTokensResponse>.Fail("TELEGRAM_INITDATA_INVALID", error ?? "Invalid Telegram initData.");

        // Existing link → sign that user in.
        var account = await dbContext.TelegramAccounts
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.TelegramUserId == tg.UserId, cancellationToken);

        User user;
        if (account is not null)
        {
            user = account.User!;
            if (user.Status != UserStatus.Active)
                return Result<AuthTokensResponse>.Fail("USER_INACTIVE", "User is not active.");

            // Keep the cached Telegram snapshot fresh on every sign-in.
            account.UpdateProfile(tg.Username, tg.FirstName, tg.LastName, tg.PhotoUrl);
        }
        else
        {
            // First-time Telegram visitor → provision a Student. The email is
            // synthetic + unique per Telegram id (never collides across users);
            // the password is random because this account only ever authenticates
            // through Telegram, not the email/password form.
            var studentRole = await dbContext.Roles
                .FirstOrDefaultAsync(x => x.Code == RoleCodes.Student, cancellationToken);
            if (studentRole is null)
                return Result<AuthTokensResponse>.Fail("ROLE_NOT_FOUND", "Student role is not configured.");

            var email = $"tg{tg.UserId}@telegram.eduvibe.local";
            var randomPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            user = new User(email, passwordHasher.Hash(randomPassword));
            await dbContext.Users.AddAsync(user, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            await dbContext.UserRoles.AddAsync(new UserRole(user.Id, studentRole.Id), cancellationToken);

            var studentProfile = new StudentProfile(user.Id, user);
            studentProfile.UpdateProfile(tg.FirstName, tg.LastName, phoneNumber: null, description: null);
            if (!string.IsNullOrWhiteSpace(tg.PhotoUrl)) studentProfile.SetAvatarUrl(tg.PhotoUrl);
            await dbContext.StudentProfiles.AddAsync(studentProfile, cancellationToken);

            account = new TelegramAccount(user.Id, tg.UserId, tg.Username, tg.FirstName, tg.LastName, tg.PhotoUrl);
            await dbContext.TelegramAccounts.AddAsync(account, cancellationToken);

            // Persist the role/profile/link BEFORE issuing the token — IssueTokens
            // reads roles/permissions back via LINQ (DB), which won't see pending
            // unsaved inserts, so a first-time token would otherwise carry no
            // role/permission claims.
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var tokens = await IssueTokensAsync(user, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<AuthTokensResponse>.Ok(tokens, "Authenticated via Telegram.");
    }

    private async Task<AuthTokensResponse> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        var roles = await dbContext.UserRoles.Where(x => x.UserId == user.Id)
            .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Code)
            .ToArrayAsync(cancellationToken);
        var permissions = await dbContext.UserRoles.Where(x => x.UserId == user.Id)
            .Join(dbContext.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp.PermissionId)
            .Join(dbContext.Permissions, pid => pid, p => p.Id, (pid, p) => p.Code)
            .Distinct()
            .ToArrayAsync(cancellationToken);

        var studentProfileId = await dbContext.StudentProfiles
            .Where(x => x.UserId == user.Id).Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var staffProfileId = await dbContext.StaffProfiles
            .Where(x => x.UserId == user.Id).Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var accessToken = jwtTokenGenerator.Generate(
            user.Id, user.Email, roles, permissions,
            studentProfileId: studentProfileId,
            staffProfileId: staffProfileId);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        user.SetRefreshToken(passwordHasher.Hash(refreshToken), dateTimeProvider.UtcNow.AddDays(30));

        return new AuthTokensResponse(user.Id, user.Email, accessToken, refreshToken, roles, permissions);
    }
}

/// <summary>
/// Connect Telegram to the already-authenticated web user. The Telegram identity
/// comes from the verified initData; the platform user comes from the JWT — never
/// the body — so a caller can only ever link Telegram to <em>themselves</em>.
/// </summary>
public sealed class TelegramLinkCommandHandler(
    IApplicationDbContext dbContext,
    ITelegramInitDataValidator validator,
    ICurrentUserService currentUser)
    : IRequestHandler<TelegramLinkCommand, Result<TelegramProfileDto>>
{
    public async Task<Result<TelegramProfileDto>> Handle(TelegramLinkCommand request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result<TelegramProfileDto>.Fail("UNAUTHENTICATED", "Caller must be authenticated.");

        var (tg, error) = validator.Validate(request.InitData);
        if (tg is null)
            return Result<TelegramProfileDto>.Fail("TELEGRAM_INITDATA_INVALID",
                error ?? "Invalid Telegram initData.");

        var userId = currentUser.UserId.Value;

        // Is this Telegram identity already attached somewhere?
        var byTelegram = await dbContext.TelegramAccounts
            .FirstOrDefaultAsync(x => x.TelegramUserId == tg.UserId, cancellationToken);
        if (byTelegram is not null && byTelegram.UserId != userId)
            return Result<TelegramProfileDto>.Fail("TELEGRAM_ALREADY_LINKED",
                "This Telegram account is already linked to another user.");

        // Does this user already have a (different) Telegram link?
        var byUser = await dbContext.TelegramAccounts
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        TelegramAccount account;
        if (byUser is not null)
        {
            account = byUser;
            account.UpdateProfile(tg.Username, tg.FirstName, tg.LastName, tg.PhotoUrl);
        }
        else
        {
            account = new TelegramAccount(userId, tg.UserId, tg.Username, tg.FirstName, tg.LastName, tg.PhotoUrl);
            await dbContext.TelegramAccounts.AddAsync(account, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<TelegramProfileDto>.Ok(ToDto(account), "Telegram linked.");
    }

    internal static TelegramProfileDto ToDto(TelegramAccount a) =>
        new(a.TelegramUserId, a.Username, a.FirstName, a.LastName, a.PhotoUrl, a.LinkedAt);
}

public sealed class GetTelegramProfileQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser)
    : IRequestHandler<GetTelegramProfileQuery, Result<TelegramProfileDto?>>
{
    public async Task<Result<TelegramProfileDto?>> Handle(GetTelegramProfileQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result<TelegramProfileDto?>.Fail("UNAUTHENTICATED", "Caller must be authenticated.");

        var account = await dbContext.TelegramAccounts
            .FirstOrDefaultAsync(x => x.UserId == currentUser.UserId.Value, cancellationToken);

        return Result<TelegramProfileDto?>.Ok(account is null ? null : TelegramLinkCommandHandler.ToDto(account));
    }
}

public sealed class UnlinkTelegramCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser)
    : IRequestHandler<UnlinkTelegramCommand, Result>
{
    public async Task<Result> Handle(UnlinkTelegramCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result.Fail("UNAUTHENTICATED", "Caller must be authenticated.");

        var account = await dbContext.TelegramAccounts
            .FirstOrDefaultAsync(x => x.UserId == currentUser.UserId.Value, cancellationToken);
        if (account is null) return Result.Ok("No Telegram link to remove.");

        dbContext.TelegramAccounts.Remove(account);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Ok("Telegram disconnected.");
    }
}
