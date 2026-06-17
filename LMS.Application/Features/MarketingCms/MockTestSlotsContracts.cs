using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.MarketingCms;

public sealed record MockTestSlotDto(
    Guid Id,
    string Title,
    DateTime StartsAt,
    string? DurationText,
    int Capacity,
    int AvailableSeats,
    int SortOrder,
    bool IsActive);

/// <summary>Admin listing — pass <c>onlyActive</c> to preview what the marketing site shows.</summary>
public sealed record GetMockTestSlotsQuery(bool OnlyActive = false)
    : IRequest<Result<IReadOnlyCollection<MockTestSlotDto>>>;

/// <summary>Public list: active + upcoming only, soonest first.</summary>
public sealed record GetPublicMockTestSlotsQuery
    : IRequest<Result<IReadOnlyCollection<MockTestSlotDto>>>;

public sealed record CreateMockTestSlotCommand(
    string Title, DateTime StartsAt, string? DurationText,
    int Capacity, int AvailableSeats, int SortOrder, bool IsActive)
    : IRequest<Result<MockTestSlotDto>>;

public sealed record UpdateMockTestSlotCommand(
    Guid SlotId,
    string Title, DateTime StartsAt, string? DurationText,
    int Capacity, int AvailableSeats, int SortOrder, bool IsActive)
    : IRequest<Result<MockTestSlotDto>>;

public sealed record DeleteMockTestSlotCommand(Guid SlotId) : IRequest<Result>;
