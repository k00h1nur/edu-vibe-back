using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// One uploaded file on a <see cref="Submission"/>. A submission can carry
/// many. Each file stores a SHA-256 of its bytes so we can (a) reject an
/// identical re-upload within the same submission and (b) flag the exact
/// same file appearing under two different students for the same assignment
/// — the core anti-cheat signal.
/// </summary>
public sealed class SubmissionFile : BaseEntity
{
    private SubmissionFile() { } // EF

    public SubmissionFile(
        Guid submissionId,
        string storedFileName,
        string originalFileName,
        string mimeType,
        long fileSize,
        string sha256)
    {
        if (submissionId == Guid.Empty) throw new DomainException("Submission id is required.");
        if (string.IsNullOrWhiteSpace(storedFileName)) throw new DomainException("Stored file name is required.");
        if (string.IsNullOrWhiteSpace(sha256)) throw new DomainException("File hash is required.");

        SubmissionId = submissionId;
        StoredFileName = storedFileName;
        OriginalFileName = string.IsNullOrWhiteSpace(originalFileName) ? storedFileName : originalFileName.Trim();
        MimeType = string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType;
        FileSize = fileSize;
        Sha256 = sha256;
    }

    public Guid SubmissionId { get; private set; }
    public Submission? Submission { get; private set; }

    public string StoredFileName { get; private set; } = null!;
    public string OriginalFileName { get; private set; } = null!;
    public string MimeType { get; private set; } = null!;
    public long FileSize { get; private set; }
    public string Sha256 { get; private set; } = null!;

    /// <summary>
    /// Set when the exact same bytes (same SHA-256) were uploaded by a
    /// DIFFERENT student for the same assignment. We flag rather than block —
    /// the teacher decides what it means; blocking could be gamed.
    /// </summary>
    public bool IsDuplicateAcrossStudents { get; private set; }

    public void FlagAsCrossStudentDuplicate()
    {
        IsDuplicateAcrossStudents = true;
        Touch();
    }
}
