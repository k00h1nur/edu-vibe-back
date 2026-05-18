using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

public sealed class ResultImage : BaseEntity
{
    public ResultImage(Guid resultId, string imageUrl, string? thumbnailUrl, bool isMain) : base()
    {
        if (resultId == Guid.Empty) throw new DomainException("Result id is required.");
        if (string.IsNullOrWhiteSpace(imageUrl)) throw new DomainException("Image url is required.");
        ResultId = resultId;
        ImageUrl = imageUrl.Trim();
        ThumbnailUrl = thumbnailUrl?.Trim();
        IsMain = isMain;
    }

    public Guid ResultId { get; private set; }
    public ResultEntry? Result { get; private set; }
    public string ImageUrl { get; private set; }
    public string? ThumbnailUrl { get; private set; }
    public bool IsMain { get; private set; }
    public bool IsDeleted { get; private set; }

    public void Replace(string imageUrl, string? thumbnailUrl)
    {
        ImageUrl = imageUrl.Trim();
        ThumbnailUrl = thumbnailUrl?.Trim();
        Touch();
    }

    public void SoftDelete() => IsDeleted = true;
}
