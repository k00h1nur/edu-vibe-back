namespace LMS.Application.Common.Abstractions;

public interface IResultImageService
{
    Task<(string ImageUrl, string ThumbnailUrl)> SaveAsync(Stream stream, string fileName, CancellationToken cancellationToken);
    Task DeleteAsync(string? imageUrl, string? thumbnailUrl, CancellationToken cancellationToken);
}
