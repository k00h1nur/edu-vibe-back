using LMS.Application.Common.Abstractions;
using Microsoft.AspNetCore.Hosting;

namespace LMS.Infrastructure.Services;

public sealed class ResultImageService : IResultImageService
{
    // Only formats we're prepared to serve back as images. Anything else
    // (.svg, .html, .exe, .pdf, ...) is rejected — UseStaticFiles would
    // happily serve them as their declared MIME type otherwise, which is a
    // stored-XSS / drive-by-download risk for an admin upload feature.
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    private readonly string _root;

    public ResultImageService(IWebHostEnvironment env)
    {
        // Save under ContentRootPath/uploads/results so the path matches what
        // Program.cs serves with UseStaticFiles. Previously this used
        // AppContext.BaseDirectory which resolves to bin/Debug/... — uploads
        // were written but never reachable.
        _root = Path.Combine(env.ContentRootPath, "uploads", "results");
        Directory.CreateDirectory(_root);
    }

    public async Task<(string ImageUrl, string ThumbnailUrl)> SaveAsync(Stream stream, string fileName,
        CancellationToken cancellationToken)
    {
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
        {
            throw new InvalidOperationException(
                $"Unsupported image extension '{ext}'. Allowed: {string.Join(", ", AllowedExtensions)}.");
        }

        // Normalize to lowercase — the URL we return is case-sensitive on Linux
        // hosts, and an arbitrary-case extension serves no purpose here.
        ext = ext.ToLowerInvariant();

        var id = Guid.NewGuid().ToString("N");
        var imageFile = $"{id}{ext}";
        var thumbFile = $"{id}_thumb{ext}";
        var imagePath = Path.Combine(_root, imageFile);
        var thumbPath = Path.Combine(_root, thumbFile);

        await using (var fs = new FileStream(imagePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await stream.CopyToAsync(fs, cancellationToken);
        }

        // Minimal MVP thumbnail generation: duplicate file as thumbnail placeholder.
        File.Copy(imagePath, thumbPath, true);

        return ($"/uploads/results/{imageFile}", $"/uploads/results/{thumbFile}");
    }

    public Task DeleteAsync(string? imageUrl, string? thumbnailUrl, CancellationToken cancellationToken)
    {
        foreach (var rel in new[] { imageUrl, thumbnailUrl })
        {
            if (string.IsNullOrWhiteSpace(rel)) continue;

            // Guard against ".." path-traversal in stored URLs (defensive — the
            // URLs are produced by SaveAsync above, but a future caller might
            // pass an attacker-controlled value).
            var safe = rel.TrimStart('/');
            if (safe.Contains("..", StringComparison.Ordinal)) continue;

            var leafName = Path.GetFileName(safe);
            if (string.IsNullOrEmpty(leafName)) continue;
            var path = Path.Combine(_root, leafName);
            if (File.Exists(path)) File.Delete(path);
        }

        return Task.CompletedTask;
    }
}
