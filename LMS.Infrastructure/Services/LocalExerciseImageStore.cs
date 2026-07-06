using LMS.Application.Common.Abstractions;
using Microsoft.AspNetCore.Hosting;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Disk-backed <see cref="IExerciseImageStore"/>. Files land under
/// <c>{ContentRoot}/uploads/exercise-images/</c> — a private bucket NOT exposed through
/// the static-file pipeline, so images are reachable only via the authenticated endpoint.
/// Raster image types only; stored under a random GUID name (no caller-supplied path).
/// </summary>
public sealed class LocalExerciseImageStore : IExerciseImageStore
{
    private static readonly Dictionary<string, string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
        [".gif"] = "image/gif",
    };

    private readonly string _root;

    public LocalExerciseImageStore(IWebHostEnvironment env)
    {
        _root = Path.Combine(env.ContentRootPath, "uploads", "exercise-images");
        if (!Directory.Exists(_root)) Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream source, string originalFileName, CancellationToken ct)
    {
        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(ext) || !Allowed.ContainsKey(ext))
            throw new InvalidOperationException(
                $"Image type '{ext}' is not allowed. Permitted: {string.Join(", ", Allowed.Keys.OrderBy(e => e))}.");

        var storedFileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var absolutePath = Path.Combine(_root, storedFileName);
        await using (var dest = File.Create(absolutePath))
        {
            await source.CopyToAsync(dest, ct);
        }
        return storedFileName;
    }

    public Task<(Stream Stream, string ContentType)?> OpenAsync(string storedFileName, CancellationToken ct)
    {
        var safe = Path.GetFileName(storedFileName);
        if (string.IsNullOrEmpty(safe) || safe != storedFileName) return Task.FromResult<(Stream, string)?>(null);
        var ext = Path.GetExtension(safe);
        if (!Allowed.TryGetValue(ext, out var mime)) return Task.FromResult<(Stream, string)?>(null);
        var absolutePath = Path.Combine(_root, safe);
        if (!File.Exists(absolutePath)) return Task.FromResult<(Stream, string)?>(null);
        Stream s = File.OpenRead(absolutePath);
        return Task.FromResult<(Stream, string)?>((s, mime));
    }
}
