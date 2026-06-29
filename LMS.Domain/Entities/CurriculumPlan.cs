using LMS.Domain.Common;

namespace LMS.Domain.Entities;

/// <summary>
/// One "class day" in a coursebook's reusable teaching plan, hanging off a
/// <see cref="CurriculumTemplate"/>. A day covers one or more curriculum lessons
/// (a paired day = 1A + 1B; an exam day = a single exam lesson). Defined once on
/// the book (template-level) and copied onto a class's editable clone so every
/// class inherits the same pairing. Rails only — handlers consume this in the
/// plan-aware generation / roadmap / materialize PRs.
/// </summary>
public sealed class CurriculumPlanDay : BaseEntity
{
    // EF materialisation ctor.
    private CurriculumPlanDay() { }

    public CurriculumPlanDay(Guid templateId, int order, string? title = null)
    {
        TemplateId = templateId;
        Order = order;
        Title = Normalize(title);
    }

    public Guid TemplateId { get; private set; }
    public int Order { get; private set; }
    public string? Title { get; private set; }

    public ICollection<CurriculumPlanDayLesson> Lessons { get; } = new List<CurriculumPlanDayLesson>();

    public void SetOrder(int order)
    {
        Order = order;
        Touch();
    }

    public void Rename(string? title)
    {
        Title = Normalize(title);
        Touch();
    }

    private static string? Normalize(string? title) =>
        string.IsNullOrWhiteSpace(title) ? null : title.Trim();
}

/// <summary>
/// A single curriculum lesson placed on a <see cref="CurriculumPlanDay"/>, with
/// its in-day order (1A=1, 1B=2). Unique per (day, lesson) so a lesson can't be
/// listed twice on the same day.
/// </summary>
public sealed class CurriculumPlanDayLesson : BaseEntity
{
    private CurriculumPlanDayLesson() { }

    public CurriculumPlanDayLesson(Guid planDayId, Guid curriculumLessonId, int order)
    {
        PlanDayId = planDayId;
        CurriculumLessonId = curriculumLessonId;
        Order = order;
    }

    public Guid PlanDayId { get; private set; }
    public Guid CurriculumLessonId { get; private set; }
    public int Order { get; private set; }
}
