using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// A single graded item inside an assignment. Holds type-specific content and
/// solution as JSON strings — the shape is documented per <see cref="LearningTaskType"/>:
///
/// <list type="bullet">
///   <item><b>MultipleChoice</b>: Content <c>{"prompt": "...", "options": ["..."], "multiple": false}</c>;
///         Solution <c>{"correctIndices": [0,2]}</c>; Response <c>{"selectedIndices": [0]}</c>.</item>
///   <item><b>FillGaps</b>: Content <c>{"text": "He ___ a doctor."}</c>;
///         Solution <c>{"fills": ["is"]}</c>; Response <c>{"fills": ["is"]}</c>.</item>
///   <item><b>Listening</b>: Content <c>{"audioUrl": "...", "prompt": "...", "format": "multiple-choice"|"short-answer"}</c>;
///         Solution depends on format; Response same.</item>
///   <item><b>ShortAnswer</b>: Content <c>{"prompt": "..."}</c>;
///         Solution <c>{"acceptedAnswers": ["..."], "caseSensitive": false}</c>;
///         Response <c>{"answer": "..."}</c>.</item>
///   <item><b>Matching</b>: Content <c>{"lefts": ["..."], "rights": ["..."]}</c>;
///         Solution <c>{"pairs": [[0,2],[1,0]]}</c>; Response same.</item>
///   <item><b>Ordering</b>: Content <c>{"items": ["..."]}</c>;
///         Solution <c>{"order": [2,0,1]}</c>; Response same.</item>
///   <item><b>Test</b>: composite — Content holds metadata, the actual sub-questions
///         are independent LearningTasks under the same AssignmentId.</item>
/// </list>
/// </summary>
public sealed class LearningTask : BaseEntity
{
    private LearningTask() { }

    public LearningTask(
        Guid assignmentId,
        int order,
        LearningTaskType type,
        string title,
        int points,
        string contentJson,
        string? solutionJson = null)
    {
        if (assignmentId == Guid.Empty) throw new DomainException("Assignment id is required.");
        if (order < 0) throw new DomainException("Order must be non-negative.");
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Task title is required.");
        if (points <= 0) throw new DomainException("Points must be greater than zero.");
        if (string.IsNullOrWhiteSpace(contentJson)) throw new DomainException("Content is required.");

        AssignmentId = assignmentId;
        Order = order;
        Type = type;
        Title = title.Trim();
        Points = points;
        ContentJson = contentJson;
        SolutionJson = string.IsNullOrWhiteSpace(solutionJson) ? null : solutionJson;
    }

    public Guid AssignmentId { get; private set; }
    public Assignment? Assignment { get; private set; }

    public int Order { get; private set; }
    public LearningTaskType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public int Points { get; private set; }
    /// <summary>JSON blob — type-specific. See class docs for shapes.</summary>
    public string ContentJson { get; private set; } = "{}";
    /// <summary>JSON blob with the correct answer; nullable for tasks that are manually graded only.</summary>
    public string? SolutionJson { get; private set; }

    public ICollection<TaskSubmission> Submissions { get; } = new List<TaskSubmission>();

    public void Update(int order, string title, int points, string contentJson, string? solutionJson)
    {
        if (order < 0) throw new DomainException("Order must be non-negative.");
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Task title is required.");
        if (points <= 0) throw new DomainException("Points must be greater than zero.");
        if (string.IsNullOrWhiteSpace(contentJson)) throw new DomainException("Content is required.");

        Order = order;
        Title = title.Trim();
        Points = points;
        ContentJson = contentJson;
        SolutionJson = string.IsNullOrWhiteSpace(solutionJson) ? null : solutionJson;
        Touch();
    }
}

public enum LearningTaskType
{
    MultipleChoice = 1,
    FillGaps = 2,
    Listening = 3,
    ShortAnswer = 4,
    Matching = 5,
    Ordering = 6,
    Test = 7,
}
