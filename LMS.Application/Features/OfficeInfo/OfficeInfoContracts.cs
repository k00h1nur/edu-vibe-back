using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.OfficeInfo;

/// <summary>
/// Public DTO for the academy's contact + branding. Read by both the admin
/// Office Info screen and the marketing site's contact section. All fields
/// are nullable except <see cref="AcademyName"/> — the academy may legally
/// have only a phone, or only social links, etc.
/// </summary>
public sealed record OfficeInfoDto(
    string AcademyName,
    string? Tagline,
    string? PhoneNumber,
    string? SecondaryPhone,
    string? Email,
    string? Address,
    string? WorkingHours,
    string? TelegramUrl,
    string? InstagramUrl,
    string? FacebookUrl,
    string? YoutubeUrl,
    string? WebsiteUrl,
    string? AboutHtml,
    DateTime UpdatedAt);

public sealed record GetOfficeInfoQuery : IRequest<Result<OfficeInfoDto>>;

public sealed record UpsertOfficeInfoCommand(
    string AcademyName,
    string? Tagline,
    string? PhoneNumber,
    string? SecondaryPhone,
    string? Email,
    string? Address,
    string? WorkingHours,
    string? TelegramUrl,
    string? InstagramUrl,
    string? FacebookUrl,
    string? YoutubeUrl,
    string? WebsiteUrl,
    string? AboutHtml) : IRequest<Result<OfficeInfoDto>>;
