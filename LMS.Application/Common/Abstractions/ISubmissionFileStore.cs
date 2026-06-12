namespace LMS.Application.Common.Abstractions;

/// <summary>Result of persisting a submission file — stored name + content hash + byte length.</summary>
public sealed record SavedSubmissionFile(string StoredFileName, string Sha256, long Size);

/// <summary>
/// Stores student submission blobs under <c>{ContentRoot}/uploads/submissions/</c>.
/// The bucket is private (not served by the static pipeline) — downloads go
/// through the controller so the self-only / staff access check runs per call.
/// SaveAsync computes the SHA-256 in the same pass it writes, so the anti-cheat
/// duplicate check costs no extra read.
/// </summary>
public interface ISubmissionFileStore
{
    Task<SavedSubmissionFile> SaveAsync(Stream source, string originalFileName, string mimeType, CancellationToken ct);
    Task<Stream?> OpenAsync(string storedFileName, CancellationToken ct);
    Task<bool> DeleteAsync(string storedFileName, CancellationToken ct);
}
