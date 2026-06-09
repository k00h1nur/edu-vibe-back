using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Announcements;

public sealed class AnnouncementsHandlers(IApplicationDbContext db) :
    IRequestHandler<GetAnnouncementsQuery, Result<IReadOnlyCollection<AnnouncementDto>>>,
    IRequestHandler<CreateAnnouncementCommand, Result<AnnouncementDto>>,
    IRequestHandler<UpdateAnnouncementCommand, Result<AnnouncementDto>>,
    IRequestHandler<SetAnnouncementPublishedCommand, Result<AnnouncementDto>>,
    IRequestHandler<DeleteAnnouncementCommand, Result>
{
    public async Task<Result<IReadOnlyCollection<AnnouncementDto>>> Handle(GetAnnouncementsQuery request, CancellationToken ct)
    {
        var query = db.Announcements.AsNoTracking();
        if (request.PublishedOnly) query = query.Where(a => a.IsPublished);

        var items = await query
            .OrderByDescending(a => a.PublishedAt ?? a.CreatedAt)
            .Select(a => new AnnouncementDto(a.Id, a.Title, a.Body, a.IsPublished, a.PublishedAt, a.AuthorUserId, a.CreatedAt))
            .ToListAsync(ct);
        return Result<IReadOnlyCollection<AnnouncementDto>>.Ok(items);
    }

    public async Task<Result<AnnouncementDto>> Handle(CreateAnnouncementCommand request, CancellationToken ct)
    {
        var entity = new Announcement(request.Title, request.Body, request.AuthorUserId);
        await db.Announcements.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
        return Result<AnnouncementDto>.Ok(Map(entity));
    }

    public async Task<Result<AnnouncementDto>> Handle(UpdateAnnouncementCommand request, CancellationToken ct)
    {
        var entity = await db.Announcements.FirstOrDefaultAsync(a => a.Id == request.AnnouncementId, ct);
        if (entity is null) return Result<AnnouncementDto>.Fail("NOT_FOUND", "Announcement not found.");
        entity.UpdateContent(request.Title, request.Body);
        await db.SaveChangesAsync(ct);
        return Result<AnnouncementDto>.Ok(Map(entity));
    }

    public async Task<Result<AnnouncementDto>> Handle(SetAnnouncementPublishedCommand request, CancellationToken ct)
    {
        var entity = await db.Announcements.FirstOrDefaultAsync(a => a.Id == request.AnnouncementId, ct);
        if (entity is null) return Result<AnnouncementDto>.Fail("NOT_FOUND", "Announcement not found.");
        if (request.IsPublished) entity.Publish();
        else entity.Unpublish();
        await db.SaveChangesAsync(ct);
        return Result<AnnouncementDto>.Ok(Map(entity));
    }

    public async Task<Result> Handle(DeleteAnnouncementCommand request, CancellationToken ct)
    {
        var entity = await db.Announcements.FirstOrDefaultAsync(a => a.Id == request.AnnouncementId, ct);
        if (entity is null) return Result.Fail("NOT_FOUND", "Announcement not found.");
        db.Announcements.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Result.Ok("Deleted");
    }

    private static AnnouncementDto Map(Announcement a) => new(
        a.Id, a.Title, a.Body, a.IsPublished, a.PublishedAt, a.AuthorUserId, a.CreatedAt);
}
