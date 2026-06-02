using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Students;

public sealed class GetStudentsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetStudentsQuery, Result<PagedResult<StudentDto>>>
{
    public async Task<Result<PagedResult<StudentDto>>> Handle(GetStudentsQuery request,
        CancellationToken cancellationToken)
    {
        var page = new PageRequest(request.Page, request.PageSize, request.Search);

        // Join to an anonymous shape and project to the DTO LAST so EF can
        // translate the whole query. Previous shape projected `new StudentDto(...)`
        // before Where/OrderBy, which EF cannot push into SQL — same bug shape
        // as StaffHandlers; was waiting for the first user to hit the page.
        var query =
            from s in db.StudentProfiles.AsNoTracking()
            join u in db.Users.AsNoTracking() on s.UserId equals u.Id
            select new { s, u };

        if (page.NormalizedSearch is { } search)
        {
            query = query.Where(x => x.u.Email.ToLower().Contains(search));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.u.Email)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .Select(x => new StudentDto(x.s.Id, x.s.UserId, x.u.Email, x.s.XP, x.s.Streak))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<StudentDto>>.Ok(PagedResult<StudentDto>.From(items, total, page));
    }
}

public sealed class GetStudentDetailQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetStudentDetailQuery, Result<StudentDto>>
{
    public async Task<Result<StudentDto>> Handle(GetStudentDetailQuery request, CancellationToken cancellationToken)
    {
        var sp = await db.StudentProfiles.Join(db.Users, s => s.UserId, u => u.Id, (s, u) => new { s, u })
            .FirstOrDefaultAsync(x => x.s.Id == request.StudentProfileId, cancellationToken);
        if (sp is null) return Result<StudentDto>.Fail("NOT_FOUND", "Student profile not found.");
        return Result<StudentDto>.Ok(new StudentDto(sp.s.Id, sp.s.UserId, sp.u.Email, sp.s.XP, sp.s.Streak));
    }
}

public sealed class RegisterStudentCommandHandler(IApplicationDbContext db)
    : IRequestHandler<RegisterStudentCommand, Result<StudentDto>>
{
    public async Task<Result<StudentDto>> Handle(RegisterStudentCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
        if (user is null) return Result<StudentDto>.Fail("NOT_FOUND", "User not found.");
        var existing = await db.StudentProfiles.FirstOrDefaultAsync(x => x.UserId == request.UserId, cancellationToken);
        if (existing is not null)
            return Result<StudentDto>.Ok(
                new StudentDto(existing.Id, existing.UserId, user.Email, existing.XP, existing.Streak),
                "Already exists.");

        var sp = new StudentProfile(user.Id, user);
        await db.StudentProfiles.AddAsync(sp, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result<StudentDto>.Ok(new StudentDto(sp.Id, sp.UserId, user.Email, sp.XP, sp.Streak));
    }
}

public sealed class UpdateStudentProfileCommandHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateStudentProfileCommand, Result<StudentDto>>
{
    public async Task<Result<StudentDto>> Handle(UpdateStudentProfileCommand request,
        CancellationToken cancellationToken)
    {
        var sp = await db.StudentProfiles.FirstOrDefaultAsync(x => x.Id == request.StudentProfileId, cancellationToken);
        if (sp is null) return Result<StudentDto>.Fail("NOT_FOUND", "Student profile not found.");
        if (request.Xp > sp.XP) sp.AddXp(request.Xp - sp.XP);
        sp.UpdateStreak(request.Streak);
        await db.SaveChangesAsync(cancellationToken);
        var user = await db.Users.FirstAsync(x => x.Id == sp.UserId, cancellationToken);
        return Result<StudentDto>.Ok(new StudentDto(sp.Id, sp.UserId, user.Email, sp.XP, sp.Streak));
    }
}

public sealed class GetMyStudentProfileQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetMyStudentProfileQuery, Result<StudentDto>>
{
    public async Task<Result<StudentDto>> Handle(GetMyStudentProfileQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result<StudentDto>.Fail("UNAUTHENTICATED", "No authenticated user.");

        // Prefer the JWT claim (free lookup); fall back to UserId scan if not yet enriched.
        var query = db.StudentProfiles
            .Join(db.Users, s => s.UserId, u => u.Id, (s, u) => new { s, u });

        var match = currentUser.StudentProfileId is { } profileId
            ? await query.FirstOrDefaultAsync(x => x.s.Id == profileId, cancellationToken)
            : await query.FirstOrDefaultAsync(x => x.s.UserId == currentUser.UserId, cancellationToken);

        if (match is null)
            return Result<StudentDto>.Fail("NOT_FOUND", "No student profile is linked to this account.");

        return Result<StudentDto>.Ok(
            new StudentDto(match.s.Id, match.s.UserId, match.u.Email, match.s.XP, match.s.Streak));
    }
}
