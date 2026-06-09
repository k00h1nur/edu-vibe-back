using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// Singleton holding academy-wide info exposed to the marketing site:
/// phone, email, address, social links, dynamic Results / Teachers /
/// hero content. There is always exactly one row, identified by the
/// fixed id <see cref="SingletonId"/>; <c>GetOrCreate</c> on the handler
/// side ensures it materialises.
/// </summary>
public sealed class OfficeInfo : BaseEntity
{
    /// <summary>Fixed id so the row is always findable without an extra "first row" query.</summary>
    public static readonly Guid SingletonId = new("00000000-0000-0000-0000-00000000FFFE");

    private OfficeInfo() { }

    public OfficeInfo(Guid id)
    {
        Id = id;
    }

    public string? AcademyName { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }

    /// <summary>Markdown / plain text shown on the marketing site's Results page.</summary>
    public string? ResultsContent { get; private set; }

    /// <summary>Markdown / plain text shown above the dynamic teacher list.</summary>
    public string? TeachersIntro { get; private set; }

    /// <summary>Plain text "About / Hero" copy used on the marketing landing.</summary>
    public string? HeroContent { get; private set; }

    public string? InstagramUrl { get; private set; }
    public string? FacebookUrl { get; private set; }
    public string? TelegramUrl { get; private set; }
    public string? YoutubeUrl { get; private set; }
    public string? TiktokUrl { get; private set; }
    public string? LinkedInUrl { get; private set; }

    public void Update(
        string? academyName,
        string? phone,
        string? email,
        string? address,
        string? resultsContent,
        string? teachersIntro,
        string? heroContent,
        string? instagramUrl,
        string? facebookUrl,
        string? telegramUrl,
        string? youtubeUrl,
        string? tiktokUrl,
        string? linkedInUrl)
    {
        AcademyName = NormalizeOrNull(academyName, 128, nameof(AcademyName));
        Phone = NormalizeOrNull(phone, 64, nameof(Phone));
        Email = NormalizeOrNull(email, 320, nameof(Email));
        Address = NormalizeOrNull(address, 512, nameof(Address));
        ResultsContent = NormalizeOrNull(resultsContent, 8000, nameof(ResultsContent));
        TeachersIntro = NormalizeOrNull(teachersIntro, 2000, nameof(TeachersIntro));
        HeroContent = NormalizeOrNull(heroContent, 2000, nameof(HeroContent));
        InstagramUrl = NormalizeUrl(instagramUrl, nameof(InstagramUrl));
        FacebookUrl = NormalizeUrl(facebookUrl, nameof(FacebookUrl));
        TelegramUrl = NormalizeUrl(telegramUrl, nameof(TelegramUrl));
        YoutubeUrl = NormalizeUrl(youtubeUrl, nameof(YoutubeUrl));
        TiktokUrl = NormalizeUrl(tiktokUrl, nameof(TiktokUrl));
        LinkedInUrl = NormalizeUrl(linkedInUrl, nameof(LinkedInUrl));
        Touch();
    }

    private static string? NormalizeOrNull(string? value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{fieldName} must be {maxLength} characters or fewer.");
        return trimmed;
    }

    private static string? NormalizeUrl(string? url, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var trimmed = url.Trim();
        if (trimmed.Length > 1024)
            throw new DomainException($"{fieldName} must be 1024 characters or fewer.");
        if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            throw new DomainException($"{fieldName} must start with http:// or https://.");
        return trimmed;
    }
}
