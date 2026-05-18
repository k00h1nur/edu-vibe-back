using LMS.Domain.Common;
using LMS.Domain.Enums;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

public sealed class ResultEntry : BaseEntity
{
    public ResultEntry(string studentFullName, ExamType examType, decimal overallScore, string language) : base()
    {
        if (string.IsNullOrWhiteSpace(studentFullName)) throw new DomainException("Student full name is required.");
        if (overallScore < 0) throw new DomainException("Overall score cannot be negative.");
        if (string.IsNullOrWhiteSpace(language)) throw new DomainException("Language is required.");
        StudentFullName = studentFullName.Trim();
        ExamType = examType;
        OverallScore = overallScore;
        Language = language.Trim();
    }

    public string StudentFullName { get; private set; }
    public string? MainImageUrl { get; private set; }
    public ExamType ExamType { get; private set; }
    public decimal OverallScore { get; private set; }
    public string? Description { get; private set; }
    public string? ImprovementText { get; private set; }
    public string? DurationText { get; private set; }
    public string? Notes { get; private set; }
    public string? BadgeIcon { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsFeatured { get; private set; }
    public bool IsPublished { get; private set; }
    public string Language { get; private set; }
    public int ViewsCount { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public ICollection<ResultScoreBreakdown> ScoreBreakdowns { get; } = new List<ResultScoreBreakdown>();
    public ICollection<ResultImage> Images { get; } = new List<ResultImage>();
    public ICollection<ResultView> Views { get; } = new List<ResultView>();

    public void Update(string studentFullName, ExamType examType, decimal overallScore, string language, string? description,
        string? improvementText, string? durationText, string? notes, string? badgeIcon, int displayOrder, bool isFeatured, bool isPublished)
    {
        if (string.IsNullOrWhiteSpace(studentFullName)) throw new DomainException("Student full name is required.");
        if (overallScore < 0) throw new DomainException("Overall score cannot be negative.");
        if (string.IsNullOrWhiteSpace(language)) throw new DomainException("Language is required.");

        StudentFullName = studentFullName.Trim();
        ExamType = examType;
        OverallScore = overallScore;
        Language = language.Trim();
        Description = description?.Trim();
        ImprovementText = improvementText?.Trim();
        DurationText = durationText?.Trim();
        Notes = notes?.Trim();
        BadgeIcon = badgeIcon?.Trim();
        DisplayOrder = displayOrder;
        IsFeatured = isFeatured;
        IsPublished = isPublished;
        Touch();
    }

    public void SetMainImage(string? imageUrl)
    {
        MainImageUrl = imageUrl;
        Touch();
    }

    public void IncrementViews()
    {
        ViewsCount++;
        Touch();
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        Touch();
    }
}
