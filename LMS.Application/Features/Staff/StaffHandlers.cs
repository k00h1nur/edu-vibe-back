using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Staff;

public sealed class GetStaffQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetStaffQuery, Result<IReadOnlyCollection<StaffDto>>>
{
    public async Task<Result<IReadOnlyCollection<StaffDto>>> Handle(GetStaffQuery request,
        CancellationToken cancellationToken)
    {
        var data = await db.StaffProfiles
            .Join(db.Users, s => s.UserId, u => u.Id, (s, u) => new StaffDto(s.Id, s.UserId, u.Email, s.EmploymentType))
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyCollection<StaffDto>>.Ok(data);
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
        return Result<StaffDto>.Ok(new StaffDto(sp.Id, sp.UserId, user.Email, sp.EmploymentType));
    }
}

public sealed class UpdateStaffProfileCommandHandler(IApplicationDbContext db)
    : IRequestHandler<UpdateStaffProfileCommand, Result<StaffDto>>
{
    public async Task<Result<StaffDto>> Handle(UpdateStaffProfileCommand request, CancellationToken cancellationToken)
    {
        var sp = await db.StaffProfiles.FirstOrDefaultAsync(x => x.Id == request.StaffProfileId, cancellationToken);
        if (sp is null) return Result<StaffDto>.Fail("NOT_FOUND", "Staff profile not found.");
        typeof(StaffProfile).GetProperty(nameof(StaffProfile.EmploymentType))!.SetValue(sp, request.EmploymentType);
        await db.SaveChangesAsync(cancellationToken);
        var user = await db.Users.FirstAsync(x => x.Id == sp.UserId, cancellationToken);
        return Result<StaffDto>.Ok(new StaffDto(sp.Id, sp.UserId, user.Email, sp.EmploymentType));
    }
}