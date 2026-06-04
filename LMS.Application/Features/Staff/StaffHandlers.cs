using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Staff;

public sealed class GetStaffQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetStaffQuery, Result<PagedResult<StaffDto>>>
{
    public async Task<Result<PagedResult<StaffDto>>> Handle(GetStaffQuery request,
        CancellationToken cancellationToken)
    {
        var page = new PageRequest(request.Page, request.PageSize, request.Search);

        // Join to an anonymous shape and project to the DTO LAST so EF can
        // translate the whole query. Previous shape projected `new StaffDto(...)`
        // before Where/OrderBy, which EF cannot push into SQL — it threw at
        // runtime on every list / search request.
        var query =
            from s in db.StaffProfiles.AsNoTracking()
            join u in db.Users.AsNoTracking() on s.UserId equals u.Id
            select new { s, u };

        if (page.NormalizedSearch is { } search)
        {
            // Match against email AND the new firstName / lastName / phone
            // fields so the office admin can search by any of them. Matches
            // the StudentProfile shape so the two list pages behave identically.
            query = query.Where(x =>
                x.u.Email.ToLower().Contains(search) ||
                (x.s.FirstName != null && x.s.FirstName.ToLower().Contains(search)) ||
                (x.s.LastName != null && x.s.LastName.ToLower().Contains(search)) ||
                (x.s.PhoneNumber != null && x.s.PhoneNumber.Contains(search)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.u.Email)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .Select(x => new StaffDto(
                x.s.Id, x.s.UserId, x.u.Email, x.s.EmploymentType,
                x.s.FirstName, x.s.LastName, x.s.PhoneNumber, x.s.Description))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<StaffDto>>.Ok(PagedResult<StaffDto>.From(items, total, page));
    }
}

public sealed class CreateStaffCommandHandler(IApplicationDbContext db)
    : IRequestHandler<CreateStaffCommand, Result<StaffDto>>
{
    public async Task<Result<StaffDto>> Handle(CreateStaffCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken);
        if (user is null) return Result<StaffDto>.Fail("NOT_FOUND", "User not found.");
        if (await db.StaffProfiles.AnyAsync(x => x.UserId == request.UserId, cancellationToken))
            return Result<StaffDto>.Fail("EXISTS", "Staff profile already exists.");

        var sp = new StaffProfile(request.UserId, request.EmploymentType);
        await db.StaffProfiles.AddAsync(sp, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result<StaffDto>.Ok(new StaffDto(sp.Id, sp.UserId, user.Email, sp.EmploymentType,
            sp.FirstName, sp.LastName, sp.PhoneNumber, sp.Description));
    }
}

public sealed class UpdateStaffProfileCommandHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateStaffProfileCommand, Result<StaffDto>>
{
    public async Task<Result<StaffDto>> Handle(UpdateStaffProfileCommand request, CancellationToken cancellationToken)
    {
        var sp = await db.StaffProfiles.FirstOrDefaultAsync(x => x.Id == request.StaffProfileId, cancellationToken);
        if (sp is null) return Result<StaffDto>.Fail("NOT_FOUND", "Staff profile not found.");
        sp.SetEmploymentType(request.EmploymentType);
        await db.SaveChangesAsync(cancellationToken);
        var user = await db.Users.FirstAsync(x => x.Id == sp.UserId, cancellationToken);
        return Result<StaffDto>.Ok(new StaffDto(sp.Id, sp.UserId, user.Email, sp.EmploymentType,
            sp.FirstName, sp.LastName, sp.PhoneNumber, sp.Description));
    }
}

public sealed class UpdateStaffDetailsCommandHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateStaffDetailsCommand, Result<StaffDto>>
{
    public async Task<Result<StaffDto>> Handle(UpdateStaffDetailsCommand request,
        CancellationToken cancellationToken)
    {
        var sp = await db.StaffProfiles.FirstOrDefaultAsync(x => x.Id == request.StaffProfileId, cancellationToken);
        if (sp is null) return Result<StaffDto>.Fail("NOT_FOUND", "Staff profile not found.");
        sp.UpdateProfile(request.FirstName, request.LastName, request.PhoneNumber, request.Description);
        await db.SaveChangesAsync(cancellationToken);
        var user = await db.Users.FirstAsync(x => x.Id == sp.UserId, cancellationToken);
        return Result<StaffDto>.Ok(new StaffDto(sp.Id, sp.UserId, user.Email, sp.EmploymentType,
            sp.FirstName, sp.LastName, sp.PhoneNumber, sp.Description));
    }
}

public sealed class GetMyStaffProfileQueryHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetMyStaffProfileQuery, Result<StaffDto>>
{
    public async Task<Result<StaffDto>> Handle(GetMyStaffProfileQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is null)
            return Result<StaffDto>.Fail("UNAUTHENTICATED", "No authenticated user.");

        var query = db.StaffProfiles
            .Join(db.Users, s => s.UserId, u => u.Id,
                (s, u) => new { s, u });

        var match = currentUser.StaffProfileId is { } profileId
            ? await query.FirstOrDefaultAsync(x => x.s.Id == profileId, cancellationToken)
            : await query.FirstOrDefaultAsync(x => x.s.UserId == currentUser.UserId, cancellationToken);

        if (match is null)
            return Result<StaffDto>.Fail("NOT_FOUND", "No staff profile is linked to this account.");

        return Result<StaffDto>.Ok(new StaffDto(
            match.s.Id, match.s.UserId, match.u.Email, match.s.EmploymentType,
            match.s.FirstName, match.s.LastName, match.s.PhoneNumber, match.s.Description));
    }
}
