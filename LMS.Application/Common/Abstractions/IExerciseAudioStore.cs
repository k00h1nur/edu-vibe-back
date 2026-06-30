namespace LMS.Application.Common.Abstractions;

/// <summary>
/// Disk-backed store for lesson-exercise (Listening) audio. Files are private — served
/// only through the authenticated streaming endpoint, never the public static pipeline.
/// </summary>
public interface IExerciseAudioStore
{
    /// <summary>Persist an uploaded audio file; returns the opaque stored file name.
    /// Throws <see cref="InvalidOperationException"/> for a disallowed extension.</summary>
    Task<string> SaveAsync(Stream source, string originalFileName, CancellationToken ct);

    /// <summary>Open a stored file for streaming (with its content type), or null if
    /// the name is invalid / missing.</summary>
    Task<(Stream Stream, string ContentType)?> OpenAsync(string storedFileName, CancellationToken ct);
}
