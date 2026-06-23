using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// Singleton row holding the academy's public contact + branding info.
/// Powers both the admin Office Info screen and the marketing site's
/// "Contact" / "Address" surfaces.
///
/// Why singleton: there is exactly one academy per deployment, so we don't
/// want this list to grow. The row id is fixed (<see cref="SingletonId"/>)
/// so admin upserts are deterministic — admin POST creates the row if it
/// doesn't exist yet, PUT updates it.
/// </summary>
public sealed class OfficeInfo : BaseEntity
{
    /// <summary>Stable id for the singleton row — referenced by the upsert flow.</summary>
    public static readonly Guid SingletonId = new("50000000-0000-0000-0000-000000000001");

    private OfficeInfo() { }

    public OfficeInfo(
        string academyName,
        string? tagline,
        string? phoneNumber,
        string? secondaryPhone,
        string? email,
        string? address,
        string? workingHours,
        string? telegramUrl,
        string? instagramUrl,
        string? facebookUrl,
        string? youtubeUrl,
        string? websiteUrl,
        string? aboutHtml,
        string? mapEmbedUrl)
    {
        Id = SingletonId;
        SetAll(academyName, tagline, phoneNumber, secondaryPhone, email, address,
            workingHours, telegramUrl, instagramUrl, facebookUrl, youtubeUrl,
            websiteUrl, aboutHtml, mapEmbedUrl);
    }

    public string AcademyName { get; private set; } = null!;
    public string? Tagline { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? SecondaryPhone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public string? WorkingHours { get; private set; }
    public string? TelegramUrl { get; private set; }
    public string? InstagramUrl { get; private set; }
    public string? FacebookUrl { get; private set; }
    public string? YoutubeUrl { get; private set; }
    public string? WebsiteUrl { get; private set; }
    /// <summary>Sanitized rich-text "about" copy — rendered on the marketing site.</summary>
    public string? AboutHtml { get; private set; }
    /// <summary>Google Maps embed URL (the iframe src) — renders the location map on the marketing site.</summary>
    public string? MapEmbedUrl { get; private set; }

    public void Update(
        string academyName,
        string? tagline,
        string? phoneNumber,
        string? secondaryPhone,
        string? email,
        string? address,
        string? workingHours,
        string? telegramUrl,
        string? instagramUrl,
        string? facebookUrl,
        string? youtubeUrl,
        string? websiteUrl,
        string? aboutHtml,
        string? mapEmbedUrl)
    {
        SetAll(academyName, tagline, phoneNumber, secondaryPhone, email, address,
            workingHours, telegramUrl, instagramUrl, facebookUrl, youtubeUrl,
            websiteUrl, aboutHtml, mapEmbedUrl);
        Touch();
    }

    private void SetAll(
        string academyName, string? tagline, string? phoneNumber, string? secondaryPhone,
        string? email, string? address, string? workingHours,
        string? telegramUrl, string? instagramUrl, string? facebookUrl,
        string? youtubeUrl, string? websiteUrl, string? aboutHtml, string? mapEmbedUrl)
    {
        if (string.IsNullOrWhiteSpace(academyName))
            throw new DomainException("Academy name is required.");
        AcademyName = academyName.Trim();
        Tagline = Trim(tagline, 256);
        PhoneNumber = Trim(phoneNumber, 64);
        SecondaryPhone = Trim(secondaryPhone, 64);
        Email = Trim(email, 320);
        Address = Trim(address, 512);
        WorkingHours = Trim(workingHours, 256);
        TelegramUrl = Trim(telegramUrl, 512);
        InstagramUrl = Trim(instagramUrl, 512);
        FacebookUrl = Trim(facebookUrl, 512);
        YoutubeUrl = Trim(youtubeUrl, 512);
        WebsiteUrl = Trim(websiteUrl, 512);
        AboutHtml = Trim(aboutHtml, 8000);
        MapEmbedUrl = Trim(mapEmbedUrl, 2000);
    }

    private static string? Trim(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var t = value.Trim();
        return t.Length > max ? t[..max] : t;
    }
}
