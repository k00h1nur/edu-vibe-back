namespace LMS.Application.Common.Abstractions;

/// <summary>Result of persisting a lesson material — stored name + byte length.</summary>
public sealed record SavedLessonMaterial(string StoredFileName, long Size);

/// <summary>
/// Stores lesson material blobs under <c>{ContentRoot}/uploads/lesson-materials/</c>.
/// Private bucket — downloads stream through the controller so the per-call
/// enrolment / teacher access check runs. Mirrors the submission file store.
/// </summary>
public interface ILessonMaterialFileStore
{
    Task<SavedLessonMaterial> SaveAsync(Stream source, string originalFileName, CancellationToken ct);
    Task<Stream?> OpenAsync(string storedFileName, CancellationToken ct);
    Task<bool> DeleteAsync(string storedFileName, CancellationToken ct);
}
