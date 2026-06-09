namespace LMS.Application.Common.Abstractions;

/// <summary>
/// Stores and retrieves material files. Implementations write to local disk
/// today; a swap to S3 / blob storage is a one-class change.
/// </summary>
public interface IMaterialFileStore
{
    /// <summary>
    /// Persists the stream and returns the stored file name (relative path
    /// inside the storage root). Caller is responsible for closing the stream.
    /// </summary>
    Task<string> SaveAsync(Stream source, string originalFileName, string mimeType, CancellationToken ct);

    /// <summary>Opens a read stream for the previously stored file.</summary>
    Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken ct);

    /// <summary>Removes the stored file. Returns true on success, false if missing.</summary>
    Task<bool> DeleteAsync(string storedFileName, CancellationToken ct);
}
