namespace LMS.Application.Common.Abstractions;

/// <summary>
/// Disk-backed store for lesson-exercise images (e.g. "label the picture" prompts —
/// flags, jobs). Files are private — served only through the authenticated endpoint,
/// never the public static pipeline. Mirrors <see cref="IExerciseAudioStore"/>.
/// </summary>
public interface IExerciseImageStore
{
    /// <summary>Persist an uploaded image; returns the opaque stored file name.
    /// Throws <see cref="InvalidOperationException"/> for a disallowed extension.</summary>
    Task<string> SaveAsync(Stream source, string originalFileName, CancellationToken ct);

    /// <summary>Open a stored image for streaming (with its content type), or null if
    /// the name is invalid / missing.</summary>
    Task<(Stream Stream, string ContentType)?> OpenAsync(string storedFileName, CancellationToken ct);
}
