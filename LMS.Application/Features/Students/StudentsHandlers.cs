using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Students;

public sealed class GetStudentsQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetStudentsQuery, Result<IReadOnlyCollection<StudentDto>>>
{
    public async Task<Result<IReadOnlyCollection<StudentDto>>> Handle(GetStudentsQuery request,
        CancellationToken cancellationToken)
    {
        var data = await db.StudentProfiles
            .Join(db.Users, s => s.UserId, u => u.Id, (s, u) => new StudentDto(s.Id, s.UserId, u.Email, s.XP, s.Streak))
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyCollection<StudentDto>>.Ok(data);
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