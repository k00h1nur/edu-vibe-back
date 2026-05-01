using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Submissions;

public sealed class SubmissionsHandlers(IApplicationDbContext db) :
    IRequestHandler<SubmitAssignmentCommand, Result<SubmissionDto>>,
    IRequestHandler<GradeSubmissionCommand, Result<SubmissionDto>>,
    IRequestHandler<GetAssignmentSubmissionsQuery, Result<IReadOnlyCollection<SubmissionDto>>>,
    IRequestHandler<GetStudentSubmissionsQuery, Result<IReadOnlyCollection<SubmissionDto>>>
{
    public async Task<Result<IReadOnlyCollection<SubmissionDto>>> Handle(GetAssignmentSubmissionsQuery request,
        CancellationToken cancellationToken)
    {
        return Result<IReadOnlyCollection<SubmissionDto>>.Ok(await db.Submissions
            .Where(x => x.AssignmentId == request.AssignmentId)
            .Select(s => new SubmissionDto(s.Id, s.AssignmentId, s.StudentProfileId, s.Content, s.Status, s.Score))
            .ToListAsync(cancellationToken));
    }

    public async Task<Result<IReadOnlyCollection<SubmissionDto>>> Handle(GetStudentSubmissionsQuery request,
        CancellationToken cancellationToken)
    {
        return Result<IReadOnlyCollection<SubmissionDto>>.Ok(await db.Submissions
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
        var list = await db.Submissions.Where(x => x.AssignmentId == request.AssignmentId)
            .ToListAsync(cancellationToken);
        var s = Submission.Create(request.AssignmentId, request.StudentProfileId, request.Content, list);
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