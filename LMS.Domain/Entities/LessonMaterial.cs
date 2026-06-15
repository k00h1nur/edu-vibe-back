using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// A file attached to a specific lesson (ClassSession). Stored in a private
/// bucket; downloads stream through the controller so the per-call enrolment /
/// teacher access check runs. One session has many materials.
/// </summary>
public sealed class LessonMaterial : BaseEntity
{
    private LessonMaterial() { }

    public LessonMaterial(
        Guid classSessionId,
        string storedFileName,
        string originalFileName,
        string mimeType,
        long fileSize,
        Guid uploadedByUserId)
    {
        if (classSessionId == Guid.Empty) throw new DomainException("Class session id is required.");
        if (string.IsNullOrWhiteSpace(storedFileName)) throw new DomainException("Stored file name is required.");
        if (string.IsNullOrWhiteSpace(originalFileName)) throw new DomainException("Original file name is required.");

        ClassSessionId = classSessionId;
        StoredFileName = storedFileName;
        OriginalFileName = originalFileName.Trim();
        MimeType = string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType;
        FileSize = fileSize;
        UploadedByUserId = uploadedByUserId;
    }

    public Guid ClassSessionId { get; private set; }
    public ClassSession? ClassSession { get; private set; }

    public string StoredFileName { get; private set; } = null!;
    public string OriginalFileName { get; private set; } = null!;
    public string MimeType { get; private set; } = null!;
    public long FileSize { get; private set; }
    public Guid UploadedByUserId { get; private set; }
}
