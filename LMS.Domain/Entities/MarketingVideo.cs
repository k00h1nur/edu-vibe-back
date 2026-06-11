using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// A video tile on the marketing site's videos page. Either a YouTube URL
/// (free hosting) or — in a future iteration — an uploaded blob. Each row
/// is admin-managed and surfaced anonymously at
/// GET /api/MarketingVideos/public.
/// </summary>
public sealed class MarketingVideo : BaseEntity
{
    private MarketingVideo() { }

    public MarketingVideo(
        string title,
        string? description,
        string videoUrl,
        string? thumbnailUrl,
        int sortOrder,
        bool isActive)
    {
        Title = NormalizeTitle(title);
        Description = Trim(description, 2000);
        VideoUrl = NormalizeUrl(videoUrl, nameof(VideoUrl));
        ThumbnailUrl = Trim(thumbnailUrl, 1024);
        SortOrder = sortOrder;
        IsActive = isActive;
    }

    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public string VideoUrl { get; private set; } = null!;
    public string? ThumbnailUrl { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(string title, string? description, string videoUrl,
        string? thumbnailUrl, int sortOrder, bool isActive)
    {
        Title = NormalizeTitle(title);
        Description = Trim(description, 2000);
        VideoUrl = NormalizeUrl(videoUrl, nameof(VideoUrl));
        ThumbnailUrl = Trim(thumbnailUrl, 1024);
        SortOrder = sortOrder;
        IsActive = isActive;
        Touch();
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Video title is required.");
        var t = title.Trim();
        if (t.Length > 256)
            throw new DomainException("Video title must be 256 characters or fewer.");
        return t;
    }

    private static string NormalizeUrl(string url, string field)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new DomainException($"{field} is required.");
        var t = url.Trim();
        if (t.Length > 1024)
            throw new DomainException($"{field} must be 1024 characters or fewer.");
        if (!Uri.TryCreate(t, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new DomainException($"{field} must be a valid http(s) URL.");
        return t;
    }

    private static string? Trim(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var t = value.Trim();
        return t.Length > max ? t[..max] : t;
    }
}
