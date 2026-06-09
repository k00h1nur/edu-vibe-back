using LMS.Domain.Common;
using LMS.Domain.Enums;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// A learning material — typically a PDF — uploaded by a teacher or admin.
/// Visibility controls who can read: Public is open to every signed-in user;
/// Private restricts access to the linked classes (<see cref="MaterialClass"/>).
/// The file itself is stored on disk; the entity records the stored path and
/// the original filename so the download is served back with the right name.
/// </summary>
public sealed class Material : BaseEntity
{
    private Material() { }

    public Material(
        string title,
        string? description,
        MaterialVisibility visibility,
        string storedFileName,
        string originalFileName,
        string mimeType,
        long fileSize,
        Guid uploadedByUserId)
    {
        Title = NormalizeTitle(title);
        Description = NormalizeDescription(description);
        Visibility = visibility;
        StoredFileName = RequireNonEmpty(storedFileName, nameof(StoredFileName), 256);
        OriginalFileName = RequireNonEmpty(originalFileName, nameof(OriginalFileName), 256);
        MimeType = RequireNonEmpty(mimeType, nameof(MimeType), 128);
        if (fileSize <= 0) throw new DomainException("File size must be positive.");
        FileSize = fileSize;
        if (uploadedByUserId == Guid.Empty) throw new DomainException("Uploader user id is required.");
        UploadedByUserId = uploadedByUserId;
    }

    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public MaterialVisibility Visibility { get; private set; }

    /// <summary>Name of the file on disk under the storage root.</summary>
    public string StoredFileName { get; private set; } = null!;

    /// <summary>Original filename the uploader chose. Used as the suggested download name.</summary>
    public string OriginalFileName { get; private set; } = null!;

    public string MimeType { get; private set; } = null!;
    public long FileSize { get; private set; }

    public Guid UploadedByUserId { get; private set; }
    public User? UploadedByUser { get; private set; }

    /// <summary>Linked classes when <see cref="Visibility"/> is Private. Empty for Public.</summary>
    public ICollection<MaterialClass> Classes { get; } = new List<MaterialClass>();

    public void UpdateDetails(string title, string? description)
    {
        Title = NormalizeTitle(title);
        Description = NormalizeDescription(description);
        Touch();
    }

    public void ChangeVisibility(MaterialVisibility visibility)
    {
        Visibility = visibility;
        // Caller is responsible for clearing or populating class links to
        // match the new visibility — the handler does that atomically.
        Touch();
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Title is required.");
        var trimmed = title.Trim();
        if (trimmed.Length > 256)
            throw new DomainException("Title must be 256 characters or fewer.");
        return trimmed;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description)) return null;
        var trimmed = description.Trim();
        if (trimmed.Length > 2000)
            throw new DomainException("Description must be 2000 characters or fewer.");
        return trimmed;
    }

    private static string RequireNonEmpty(string value, string field, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{field} is required.");
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{field} must be {maxLength} characters or fewer.");
        return trimmed;
    }
}
