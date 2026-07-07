using LMS.Application.Common.Abstractions;

namespace LMS.Application.Common.Storage;

/// <summary>
/// Shared helper for deleting a previously-stored avatar file when it is
/// replaced or removed, so old images never accumulate on disk. It only acts on
/// our own locally-served paths (<c>/uploads/avatars/&lt;name&gt;</c>); anything
/// else — an absolute external URL (e.g. a cached Telegram photo), a blank, or an
/// unchanged value — is deliberately left untouched.
/// </summary>
public static class AvatarCleanup
{
    private const string LocalPrefix = "/uploads/avatars/";

    /// <summary>
    /// Best-effort delete of <paramref name="previousUrl"/>'s backing file once a
    /// new value (<paramref name="currentUrl"/>, possibly null on removal) has been
    /// committed. Safe to call unconditionally — it self-filters and never throws.
    /// </summary>
    public static async Task DeletePreviousAsync(
        IAvatarFileStore store, string? previousUrl, string? currentUrl, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(previousUrl)) return;
        if (string.Equals(previousUrl, currentUrl, StringComparison.Ordinal)) return;
        if (!previousUrl.StartsWith(LocalPrefix, StringComparison.OrdinalIgnoreCase)) return;

        var storedName = previousUrl[LocalPrefix.Length..];
        if (string.IsNullOrWhiteSpace(storedName)) return;

        // DeleteAsync path-guards the name and no-ops when the file is already gone.
        await store.DeleteAsync(storedName, ct);
    }
}
