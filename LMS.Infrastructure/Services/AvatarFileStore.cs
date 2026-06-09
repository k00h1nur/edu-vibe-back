using LMS.Application.Common.Abstractions;
using Microsoft.AspNetCore.Hosting;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Disk-backed implementation of <see cref="IAvatarFileStore"/>. Files land
/// under <c>{ContentRoot}/uploads/avatars/</c>; the static-file pipeline
/// already serves the parent <c>/uploads</c> path so the avatar is reachable
/// at <c>/uploads/avatars/&lt;storedName&gt;</c> immediately after save.
/// </summary>
public sealed class LocalAvatarFileStore : IAvatarFileStore
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".webp", ".gif",
    };

    private readonly string _root;

    public LocalAvatarFileStore(IWebHostEnvironment env)
    {
        _root = Path.Combine(env.ContentRootPath, "uploads", "avatars");
        if (!Directory.Exists(_root)) Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream source, string originalFileName, string mimeType, CancellationToken ct)
    {
        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            throw new InvalidOperationException(
                $"Avatar extension '{ext}' is not allowed. Permitted: {string.Join(", ", AllowedExtensions.OrderBy(e => e))}.");

        var storedFileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var absolutePath = Path.Combine(_root, storedFileName);

        await using var dest = File.Create(absolutePath);
        await source.CopyToAsync(dest, ct);
        return storedFileName;
    }

    public Task<bool> DeleteAsync(string storedFileName, CancellationToken ct)
    {
        var safe = Path.GetFileName(storedFileName);
        if (string.IsNullOrEmpty(safe) || safe != storedFileName) return Task.FromResult(false);
        var absolutePath = Path.Combine(_root, safe);
        if (!File.Exists(absolutePath)) return Task.FromResult(false);
        try { File.Delete(absolutePath); return Task.FromResult(true); }
        catch { return Task.FromResult(false); }
    }
}
