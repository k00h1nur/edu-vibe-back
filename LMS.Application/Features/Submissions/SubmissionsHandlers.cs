using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Submissions;

public sealed class SubmissionsHandlers(IApplicationDbContext db, ICurrentUserService currentUser) :
    IRequestHandler<SubmitAssignmentCommand, Result<SubmissionDto>>,
    IRequestHandler<GradeSubmissionCommand, Result<SubmissionDto>>,
    IRequestHandler<GetAssignmentSubmissionsQuery, Result<IReadOnlyCollection<SubmissionDto>>>,
    IRequestHandler<GetStudentSubmissionsQuery, Result<IReadOnlyCollection<SubmissionDto>>>
{
    public async Task<Result<IReadOnlyCollection<SubmissionDto>>> Handle(GetAssignmentSubmissionsQuery request,
        CancellationToken cancellationToken)
    {
        // SECURITY: students can only see their OWN submissions for this
        // assignment. Staff (anyone with a StaffProfileId in the JWT — teacher,
        // support, office, director, admin) gets the full roster of
        // submissions for grading. Without this check anyone holding
        // Submissions.Read could enumerate the whole table by assignment id.
        var items = await db.Submissions
            .AsNoTracking()
            .Where(x => x.AssignmentId == request.AssignmentId)
            .Select(s => new SubmissionDto(s.Id, s.AssignmentId, s.StudentProfileId, s.Content, s.Status, s.Score))
            .ToListAsync(cancellationToken);

        if (currentUser.StaffProfileId is null)
        {
            var ownProfileId = currentUser.StudentProfileId;
            if (ownProfileId is null)
                return Result<IReadOnlyCollection<SubmissionDto>>.Fail(
                    "FORBIDDEN", "Caller is neither a student nor staff.");
            items = items.Where(s => s.StudentProfileId == ownProfileId).ToList();
        }

        return Result<IReadOnlyCollection<SubmissionDto>>.Ok(items);
    }

    public async Task<Result<IReadOnlyCollection<SubmissionDto>>> Handle(GetStudentSubmissionsQuery request,
        CancellationToken cancellationToken)
    {
        // SECURITY: students see only their own; staff can read anyone's.
        if (currentUser.StaffProfileId is null)
        {
            if (currentUser.StudentProfileId is null)
                return Result<IReadOnlyCollection<SubmissionDto>>.Fail(
                    "FORBIDDEN", "Caller is neither a student nor staff.");
            if (currentUser.StudentProfileId != request.StudentProfileId)
                return Result<IReadOnlyCollection<SubmissionDto>>.Fail(
                    "FORBIDDEN", "Students may only read their own submissions.");
        }

        return Result<IReadOnlyCollection<SubmissionDto>>.Ok(await db.Submissions
            .AsNoTracking()
            .Where(x => x.StudentProfileId == request.StudentProfileId).Select(s =>
                new SubmissionDto(s.Id, s.AssignmentId, s.StudentProfileId, s.Content, s.Status, s.Score))
            .ToListAsync(cancellationToken));
    }

    public async Task<Result<SubmissionDto>> Handle(GradeSubmissionCommand request, CancellationToken cancellationToken)
    {
        var s = await db.Submissions.FirstOrDefaultAsync(x => x.Id == request.SubmissionId, cancellationToken);
        if (s is null) return Result<SubmissionDto>.Fail("NOT_FOUND", "Submission not found.");
        s.Grade(request.Score);
        await db.SaveChangesAsync(cancellationToken);
        return Result<SubmissionDto>.Ok(Map(s));
    }

    public async Task<Result<SubmissionDto>> Handle(SubmitAssignmentCommand request,
        CancellationToken cancellationToken)
    {
        // SECURITY: ignore the StudentProfileId on the wire — always force the
        // submission onto the caller's own student profile. Without this a
        // student could submit work as anyone else just by passing a different
        // id in the body. Submissions.Create is only granted to the Student
        // role, so anyone reaching this handler must have a student profile.
        var callerStudentProfileId = currentUser.StudentProfileId;
        if (callerStudentProfileId is null)
            return Result<SubmissionDto>.Fail("FORBIDDEN", "Only students may submit assignments.");

        var list = await db.Submissions.Where(x => x.AssignmentId == request.AssignmentId)
            .ToListAsync(cancellationToken);
        var s = Submission.Create(request.AssignmentId, callerStudentProfileId.Value, request.Content, list);
        s.Submit(request.Content, request.IsLate);
        await db.Submissions.AddAsync(s, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result<SubmissionDto>.Ok(Map(s));
    }

    private static SubmissionDto Map(Submission s)
    {
        return new SubmissionDto(s.Id, s.AssignmentId, s.StudentProfileId, s.Content, s.Status, s.Score);
    }
}