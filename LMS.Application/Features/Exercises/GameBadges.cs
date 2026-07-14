namespace LMS.Application.Features.Exercises;

/// <summary>
/// Which game-badge codes a student qualifies for after an exercise-submit event.
/// Pure + threshold-based (uses ≥ so a milestone can't be "skipped" past); the
/// caller awards only the ones not already held, so each is granted once. Codes
/// match the seeded <c>badges.Code</c> values (see the ExerciseGameBadges migration).
/// </summary>
public static class GameBadges
{
    public const string FirstSteps = "GAME_FIRST";     // first XP-earning exercise
    public const string Streak7 = "GAME_STREAK_7";     // 7-day practice streak
    public const string Streak30 = "GAME_STREAK_30";   // 30-day practice streak

    public static IEnumerable<string> EarnedFor(bool earnedXp, int streak)
    {
        if (earnedXp) yield return FirstSteps;
        if (streak >= 7) yield return Streak7;
        if (streak >= 30) yield return Streak30;
    }
}
