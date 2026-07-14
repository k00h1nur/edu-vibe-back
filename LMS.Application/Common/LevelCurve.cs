namespace LMS.Application.Common;

/// <summary>
/// Pure XP → numeric level math for the game. Level L is reached at cumulative
/// XP = 50·(L-1)² — so the gaps grow 50, 150, 250, 350… (level 2 @ 50 XP, level 3
/// @ 200, level 4 @ 450, …). Level 1 is the floor. Kept UI-free + unit-testable;
/// the numeric level is DERIVED from <c>StudentProfile.XP</c> (no stored column,
/// and unrelated to the CEFR text <c>Level</c> label).
/// </summary>
public static class LevelCurve
{
    /// <summary>Cumulative XP required to reach <paramref name="level"/> (level ≤ 1 ⇒ 0).</summary>
    public static int XpForLevel(int level) => level <= 1 ? 0 : 50 * (level - 1) * (level - 1);

    /// <summary>The level a student with <paramref name="xp"/> is currently at (≥ 1).</summary>
    public static int LevelFor(int xp)
    {
        if (xp <= 0) return 1;
        return (int)Math.Floor(Math.Sqrt(xp / 50.0)) + 1;
    }

    /// <summary>Progress toward the next level: (level, XP earned into this level, level span).</summary>
    public static (int Level, int IntoLevel, int Span) Progress(int xp)
    {
        var level = LevelFor(xp);
        var floorXp = XpForLevel(level);
        var nextXp = XpForLevel(level + 1);
        return (level, Math.Max(0, xp - floorXp), nextXp - floorXp);
    }
}
