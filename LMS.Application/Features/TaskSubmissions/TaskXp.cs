namespace LMS.Application.Features.TaskSubmissions;

/// <summary>
/// Pure XP-for-grade math (F4): proportional partial credit, round-half-up. Kept
/// out of the handler so it's unit-testable. <paramref name="score"/> is the
/// submission's 0..1 grade. Returns 0 for non-positive points/score (the caller
/// must skip the award then — <c>StudentProfile.AddXp</c> rejects ≤ 0).
/// </summary>
public static class TaskXp
{
    public static int ForGrade(int points, decimal score)
    {
        if (points <= 0 || score <= 0m) return 0;
        var clamped = score > 1m ? 1m : score;
        return (int)Math.Round(points * clamped, MidpointRounding.AwayFromZero);
    }
}
