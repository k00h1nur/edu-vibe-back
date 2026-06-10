namespace LMS.Application.Common.Abstractions;

/// <summary>
/// Stores material blobs (PDFs, slides, audio, etc). Implementations write to
/// disk today under <c>{ContentRoot}/uploads/materials/</c>; the
/// <c>MaterialsController.Download</c> endpoint streams them back so we can
/// keep the bucket private + apply access checks per call.
/// </summary>
public interface IMaterialFileStore
{
    /// <summary>
    /// Persists <paramref name="source"/> and returns the opaque stored file
    /// name (a GUID + sanitised extension) — what gets saved on the entity.
    /// </summary>
    Task<string> SaveAsync(Stream source, string originalFileName, string mimeType, CancellationToken ct);

    /// <summary>
    /// Opens the stored blob for streaming. Returns null if the file is
    /// missing on disk (eg deleted out-of-band) so the caller can 404.
    /// </summary>
    Task<Stream?> OpenAsync(string storedFileName, CancellationToken ct);

    Task<bool> DeleteAsync(string storedFileName, CancellationToken ct);
}
