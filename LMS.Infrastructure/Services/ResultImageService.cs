using LMS.Application.Common.Abstractions;

namespace LMS.Infrastructure.Services;

public sealed class ResultImageService : IResultImageService
{
    private readonly string _root;

    public ResultImageService()
    {
        _root = Path.Combine(AppContext.BaseDirectory, "uploads", "results");
        Directory.CreateDirectory(_root);
    }

    public async Task<(string ImageUrl, string ThumbnailUrl)> SaveAsync(Stream stream, string fileName,
        CancellationToken cancellationToken)
    {
        var ext = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";
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
            var normalized = rel.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var path = Path.Combine(AppContext.BaseDirectory, normalized);
            if (File.Exists(path)) File.Delete(path);
        }

        return Task.CompletedTask;
    }
}
