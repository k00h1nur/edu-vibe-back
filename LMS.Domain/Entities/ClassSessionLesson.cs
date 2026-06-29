using LMS.Domain.Common;

namespace LMS.Domain.Entities;

/// <summary>
/// Join row letting one <see cref="ClassSession"/> teach more than one curriculum
/// lesson (a class day = 1A + 1B). The existing single
/// <see cref="ClassSession.CurriculumLessonId"/> stays as the denormalised
/// "primary" for back-compat; this is the authoritative many-side. Unique per
/// (session, lesson) and ordered within the session (1A=1, 1B=2).
/// </summary>
public sealed class ClassSessionLesson : BaseEntity
{
    private ClassSessionLesson() { }

    public ClassSessionLesson(Guid classSessionId, Guid curriculumLessonId, int order)
    {
        ClassSessionId = classSessionId;
        CurriculumLessonId = curriculumLessonId;
        Order = order;
    }

    public Guid ClassSessionId { get; private set; }
    public Guid CurriculumLessonId { get; private set; }
    public int Order { get; private set; }

    public void SetOrder(int order)
    {
        Order = order;
        Touch();
    }
}
