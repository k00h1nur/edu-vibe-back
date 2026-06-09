using LMS.Application.Common.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Disk-backed implementation of <see cref="IMaterialFileStore"/>. Files land
/// under <c>{ContentRoot}/materials/</c> with a GUID-generated name so two
/// uploads with the same original filename never collide. The original
/// filename is preserved on the entity, not the disk.
/// </summary>
public sealed class LocalMaterialFileStore : IMaterialFileStore
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".ppt", ".pptx", ".xls", ".xlsx",
        ".txt", ".md",
        ".png", ".jpg", ".jpeg", ".webp", ".gif",
        ".mp3", ".mp4", ".m4a", ".wav",
        ".zip",
    };

    private readonly string _root;
    private readonly ILogger<LocalMaterialFileStore> _logger;

    public LocalMaterialFileStore(IWebHostEnvironment env, ILogger<LocalMaterialFileStore> logger)
    {
        _root = Path.Combine(env.ContentRootPath, "materials");
        if (!Directory.Exists(_root)) Directory.CreateDirectory(_root);
        _logger = logger;
    }

    public async Task<string> SaveAsync(Stream source, string originalFileName, string mimeType, CancellationToken ct)
    {
        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            throw new InvalidOperationException(
                $"File extension '{ext}' is not allowed. Permitted: {string.Join(", ", AllowedExtensions.OrderBy(e => e))}.");

        var storedFileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var absolutePath = Path.Combine(_root, storedFileName);

        await using var dest = File.Create(absolutePath);
        await source.CopyToAsync(dest, ct);

        _logger.LogInformation("Stored material file {Stored} (original={Original}, mime={Mime})",
            storedFileName, originalFileName, mimeType);
        return storedFileName;
    }

    public Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken ct)
    {
        // Defense-in-depth: refuse any path that escapes the storage root.
        var safe = Path.GetFileName(storedFileName);
        if (string.IsNullOrEmpty(safe) || safe != storedFileName) return Task.FromResult<Stream?>(null);

        var absolutePath = Path.Combine(_root, safe);
        if (!File.Exists(absolutePath)) return Task.FromResult<Stream?>(null);

        Stream s = File.OpenRead(absolutePath);
        return Task.FromResult<Stream?>(s);
    }

    public Task<bool> DeleteAsync(string storedFileName, CancellationToken ct)
    {
        var safe = Path.GetFileName(storedFileName);
        if (string.IsNullOrEmpty(safe) || safe != storedFileName) return Task.FromResult(false);

        var absolutePath = Path.Combine(_root, safe);
        if (!File.Exists(absolutePath)) return Task.FromResult(false);
        try
        {
            File.Delete(absolutePath);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete material file {Stored}", storedFileName);
            return Task.FromResult(false);
        }
    }
}
