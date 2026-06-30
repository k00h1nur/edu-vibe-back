using LMS.Application.Common.Abstractions;
using Microsoft.AspNetCore.Hosting;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Disk-backed <see cref="IExerciseAudioStore"/>. Files land under
/// <c>{ContentRoot}/uploads/exercise-audio/</c> — a private bucket NOT exposed through
/// the static-file pipeline (its mime types aren't in the static allow-list), so audio
/// is reachable only via the authenticated streaming endpoint. Audio types only.
/// </summary>
public sealed class LocalExerciseAudioStore : IExerciseAudioStore
{
    private static readonly Dictionary<string, string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        [".mp3"] = "audio/mpeg",
        [".m4a"] = "audio/mp4",
        [".aac"] = "audio/aac",
        [".ogg"] = "audio/ogg",
        [".oga"] = "audio/ogg",
        [".wav"] = "audio/wav",
        [".webm"] = "audio/webm",
    };

    private readonly string _root;

    public LocalExerciseAudioStore(IWebHostEnvironment env)
    {
        _root = Path.Combine(env.ContentRootPath, "uploads", "exercise-audio");
        if (!Directory.Exists(_root)) Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Stream source, string originalFileName, CancellationToken ct)
    {
        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(ext) || !Allowed.ContainsKey(ext))
            throw new InvalidOperationException(
                $"Audio type '{ext}' is not allowed. Permitted: {string.Join(", ", Allowed.Keys.OrderBy(e => e))}.");

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
