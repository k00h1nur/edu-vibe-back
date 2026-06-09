namespace LMS.Application.Common.Abstractions;

/// <summary>
/// Stores user avatar images. Implementations write to disk today and are
/// served by the static-file pipeline at /uploads/avatars/&lt;name&gt;.
/// </summary>
public interface IAvatarFileStore
{
    Task<string> SaveAsync(Stream source, string originalFileName, string mimeType, CancellationToken ct);
    Task<bool> DeleteAsync(string storedFileName, CancellationToken ct);
}
