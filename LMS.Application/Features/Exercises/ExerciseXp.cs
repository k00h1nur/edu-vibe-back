namespace LMS.Application.Features.Exercises;

/// <summary>
/// Pure XP math for fully completing a self-check <c>LessonExercise</c> in game mode.
/// Awarded ONCE per exercise (see <c>LessonExerciseSubmission.XpAwarded</c>), only on a
/// perfect score. 2 XP per gradable slot, floored at 5 and capped at 40 so a huge
/// exercise can't dwarf everything else. Kept out of the handler so it's unit-testable.
/// <paramref name="total"/> is the exercise's gradable-slot count; 0 ⇒ no award.
/// </summary>
public static class ExerciseXp
{
    public static int ForCompletion(int total)
    {
        if (total <= 0) return 0;
        return Math.Clamp(total * 2, 5, 40);
    }
}
