using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>Broad family a curriculum template belongs to — used for grouping/filtering in the picker.</summary>
public enum CurriculumCategory
{
    GeneralEnglish = 1,
    Ielts = 2,
    Sat = 3,
    Cefr = 4,
    Custom = 5,
}

/// <summary>Pedagogical kind of a lesson — drives the badge/icon and lets the planner balance skills.</summary>
public enum CurriculumLessonType
{
    General = 0,
    Speaking = 1,
    Grammar = 2,
    Vocabulary = 3,
    Listening = 4,
    Reading = 5,
    Writing = 6,
    Practice = 7,
    Exam = 8,
}

/// <summary>
/// A reusable learning path a class can follow:
/// Template → Module → Unit → Lesson. A class points at a template
/// (<see cref="Class.CurriculumTemplateId"/>) and each scheduled
/// <see cref="ClassSession"/> links to a <see cref="CurriculumLesson"/>, so every
/// session knows its module / unit / topic / objective. Built-in ("system")
/// templates are clone-only; clones and hand-made templates are editable.
/// </summary>
public sealed class CurriculumTemplate : BaseEntity
{
    private CurriculumTemplate() { } // EF

    public CurriculumTemplate(string name, CurriculumCategory category, string? level, string? description, bool isSystem)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Template name is required.");
        Name = name.Trim();
        Category = category;
        Level = string.IsNullOrWhiteSpace(level) ? null : level.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsSystem = isSystem;
        IsPublished = true;
    }

    public string Name { get; private set; } = null!;
    public CurriculumCategory Category { get; private set; }
    /// <summary>CEFR/level tag, e.g. "A1", "B2", "Foundation". Optional.</summary>
    public string? Level { get; private set; }
    public string? Description { get; private set; }
    /// <summary>Built-in template seeded by the platform — protected from edits, offered for cloning.</summary>
    public bool IsSystem { get; private set; }
    public bool IsPublished { get; private set; }

    public ICollection<CurriculumModule> Modules { get; } = new List<CurriculumModule>();

    public void Update(string name, CurriculumCategory category, string? level, string? description)
    {
        if (IsSystem) throw new DomainException("Built-in templates can't be edited — clone one to customise.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Template name is required.");
        Name = name.Trim();
        Category = category;
        Level = string.IsNullOrWhiteSpace(level) ? null : level.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Touch();
    }

    public void SetPublished(bool published) { IsPublished = published; Touch(); }
}

public sealed class CurriculumModule : BaseEntity
{
    private CurriculumModule() { } // EF

    public CurriculumModule(Guid templateId, int order, string title)
    {
        if (templateId == Guid.Empty) throw new DomainException("Template id is required.");
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Module title is required.");
        TemplateId = templateId;
        Order = order;
        Title = title.Trim();
    }

    public Guid TemplateId { get; private set; }
    public int Order { get; private set; }
    public string Title { get; private set; } = null!;

    public ICollection<CurriculumUnit> Units { get; } = new List<CurriculumUnit>();
}

public sealed class CurriculumUnit : BaseEntity
{
    private CurriculumUnit() { } // EF

    public CurriculumUnit(Guid moduleId, int order, string title)
    {
        if (moduleId == Guid.Empty) throw new DomainException("Module id is required.");
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Unit title is required.");
        ModuleId = moduleId;
        Order = order;
        Title = title.Trim();
    }

    public Guid ModuleId { get; private set; }
    public int Order { get; private set; }
    public string Title { get; private set; } = null!;
    /// <summary>Short description shown on the unit card. Optional.</summary>
    public string? Description { get; private set; }
    /// <summary>Optional emoji/icon shown on the unit card (e.g. "📘").</summary>
    public string? Icon { get; private set; }
    /// <summary>Estimated time to finish the unit, in minutes. Optional.</summary>
    public int? EstimatedMinutes { get; private set; }
    /// <summary>XP awarded for completing the whole unit.</summary>
    public int XpReward { get; private set; }

    public ICollection<CurriculumLesson> Lessons { get; } = new List<CurriculumLesson>();

    public void Update(string title, string? description)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Unit title is required.");
        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Touch();
    }

    public void SetOrder(int order)
    {
        Order = order;
        Touch();
    }

    /// <summary>Sets the card metadata (icon / estimated duration / XP reward).</summary>
    public void SetMeta(string? icon, int? estimatedMinutes, int xpReward)
    {
        Icon = string.IsNullOrWhiteSpace(icon) ? null : icon.Trim();
        EstimatedMinutes = estimatedMinutes is > 0 ? estimatedMinutes : null;
        XpReward = xpReward < 0 ? 0 : xpReward;
        Touch();
    }
}

/// <summary>
/// One teachable lesson in the path — the unit of work a scheduled session maps
/// to. Carries the topic, learning objectives and content "placeholders"
/// (what homework / materials / assessment the lesson expects) so the generated
/// course has slots ready for the teacher to fill.
/// </summary>
public sealed class CurriculumLesson : BaseEntity
{
    private CurriculumLesson() { } // EF

    public CurriculumLesson(
        Guid unitId, int order, string title,
        string? objectives, string? homeworkPlaceholder, string? materialsPlaceholder, bool isAssessment)
    {
        if (unitId == Guid.Empty) throw new DomainException("Unit id is required.");
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Lesson title is required.");
        UnitId = unitId;
        Order = order;
        Title = title.Trim();
        Objectives = string.IsNullOrWhiteSpace(objectives) ? null : objectives.Trim();
        HomeworkPlaceholder = string.IsNullOrWhiteSpace(homeworkPlaceholder) ? null : homeworkPlaceholder.Trim();
        MaterialsPlaceholder = string.IsNullOrWhiteSpace(materialsPlaceholder) ? null : materialsPlaceholder.Trim();
        IsAssessment = isAssessment;
    }

    public Guid UnitId { get; private set; }
    public int Order { get; private set; }
    /// <summary>The lesson topic — copied onto the linked session's Topic at generation time.</summary>
    public string Title { get; private set; } = null!;
    /// <summary>Learning objectives, newline-separated. Optional.</summary>
    public string? Objectives { get; private set; }
    public string? HomeworkPlaceholder { get; private set; }
    public string? MaterialsPlaceholder { get; private set; }
    /// <summary>Marks an assessment lesson (quiz / mock exam / test) for milestone reporting.</summary>
    public bool IsAssessment { get; private set; }
    /// <summary>Pedagogical kind — drives the lesson badge/icon and skill balancing.</summary>
    public CurriculumLessonType LessonType { get; private set; }
    /// <summary>Planned lesson length in minutes. Optional.</summary>
    public int? DurationMinutes { get; private set; }
    /// <summary>XP awarded for completing the lesson.</summary>
    public int XpReward { get; private set; }

    public void Update(string title, string? objectives, string? homeworkPlaceholder,
        string? materialsPlaceholder, bool isAssessment)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Lesson title is required.");
        Title = title.Trim();
        Objectives = string.IsNullOrWhiteSpace(objectives) ? null : objectives.Trim();
        HomeworkPlaceholder = string.IsNullOrWhiteSpace(homeworkPlaceholder) ? null : homeworkPlaceholder.Trim();
        MaterialsPlaceholder = string.IsNullOrWhiteSpace(materialsPlaceholder) ? null : materialsPlaceholder.Trim();
        IsAssessment = isAssessment;
        Touch();
    }

    public void SetOrder(int order)
    {
        Order = order;
        Touch();
    }

    /// <summary>Sets the lesson metadata (type / duration / XP reward).</summary>
    public void SetMeta(CurriculumLessonType lessonType, int? durationMinutes, int xpReward)
    {
        LessonType = lessonType;
        DurationMinutes = durationMinutes is > 0 ? durationMinutes : null;
        XpReward = xpReward < 0 ? 0 : xpReward;
        Touch();
    }

    /// <summary>Reassigns the lesson to another unit at the given order (drag-between-units).</summary>
    public void MoveToUnit(Guid unitId, int order)
    {
        if (unitId == Guid.Empty) throw new DomainException("Unit id is required.");
        UnitId = unitId;
        Order = order;
        Touch();
    }
}
