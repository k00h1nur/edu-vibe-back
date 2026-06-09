using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.OfficeInfo;

public sealed record OfficeInfoDto(
    string? AcademyName,
    string? Phone,
    string? Email,
    string? Address,
    string? ResultsContent,
    string? TeachersIntro,
    string? HeroContent,
    string? InstagramUrl,
    string? FacebookUrl,
    string? TelegramUrl,
    string? YoutubeUrl,
    string? TiktokUrl,
    string? LinkedInUrl);

public sealed record GetOfficeInfoQuery : IRequest<Result<OfficeInfoDto>>;

public sealed record UpdateOfficeInfoCommand(
    string? AcademyName,
    string? Phone,
    string? Email,
    string? Address,
    string? ResultsContent,
    string? TeachersIntro,
    string? HeroContent,
    string? InstagramUrl,
    string? FacebookUrl,
    string? TelegramUrl,
    string? YoutubeUrl,
    string? TiktokUrl,
    string? LinkedInUrl) : IRequest<Result<OfficeInfoDto>>;
