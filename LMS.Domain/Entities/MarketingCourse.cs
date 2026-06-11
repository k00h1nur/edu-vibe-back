using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// Marketing-site course card. Editable from the admin Courses page,
/// surfaced anonymously at GET /api/MarketingCourses/public. Distinct
/// from <see cref="Course"/> (the LMS-side reference catalogue) — this
/// one is purely public-facing copy.
/// </summary>
public sealed class MarketingCourse : BaseEntity
{
    private MarketingCourse() { }

    public MarketingCourse(
        string slug,
        string title,
        string? subtitle,
        string? description,
        string? coverImageUrl,
        string? priceText,
        string? durationText,
        string? levelText,
        int sortOrder,
        bool isActive)
    {
        Slug = NormalizeSlug(slug);
        Title = NormalizeTitle(title);
        Subtitle = Trim(subtitle, 256);
        Description = Trim(description, 4000);
        CoverImageUrl = Trim(coverImageUrl, 1024);
        PriceText = Trim(priceText, 64);
        DurationText = Trim(durationText, 64);
        LevelText = Trim(levelText, 64);
        SortOrder = sortOrder;
        IsActive = isActive;
    }

    public string Slug { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string? Subtitle { get; private set; }
    public string? Description { get; private set; }
    public string? CoverImageUrl { get; private set; }
    public string? PriceText { get; private set; }
    public string? DurationText { get; private set; }
    public string? LevelText { get; private set; }
    /// <summary>Lower numbers float to the top of the listing.</summary>
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(
        string slug, string title, string? subtitle, string? description,
        string? coverImageUrl, string? priceText, string? durationText,
        string? levelText, int sortOrder, bool isActive)
    {
        Slug = NormalizeSlug(slug);
        Title = NormalizeTitle(title);
        Subtitle = Trim(subtitle, 256);
        Description = Trim(description, 4000);
        CoverImageUrl = Trim(coverImageUrl, 1024);
        PriceText = Trim(priceText, 64);
        DurationText = Trim(durationText, 64);
        LevelText = Trim(levelText, 64);
        SortOrder = sortOrder;
        IsActive = isActive;
        Touch();
    }

    private static string NormalizeSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("Course slug is required.");
        var t = slug.Trim().ToLowerInvariant();
        if (t.Length > 64)
            throw new DomainException("Course slug must be 64 characters or fewer.");
        return t;
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Course title is required.");
        var t = title.Trim();
        if (t.Length > 256)
            throw new DomainException("Course title must be 256 characters or fewer.");
        return t;
    }

    private static string? Trim(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var t = value.Trim();
        return t.Length > max ? t[..max] : t;
    }
}
