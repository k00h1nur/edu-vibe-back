namespace LMS.Application.Features.Exams;

/// <summary>One section's entered score paired with its configured max.</summary>
public sealed record ExamSectionInput(decimal Score, decimal MaxScore);

public sealed record ExamScore(decimal OverallPercent, bool Passed);

/// <summary>
/// Pure, DB-free offline-exam scoring (F8): overall% = Σscore / Σmax × 100, and
/// pass = overall% >= threshold (exactly at the threshold PASSES — "60% to pass"
/// means 60 passes). Kept out of the handler so it's fully unit-testable.
/// </summary>
public static class ExamScoring
{
    /// <summary>True iff there is ≥1 section and every score is within [0, max] with a positive max.</summary>
    public static bool AreScoresValid(IReadOnlyCollection<ExamSectionInput> sections) =>
        sections.Count > 0 && sections.All(s => s.MaxScore > 0m && s.Score >= 0m && s.Score <= s.MaxScore);

    public static ExamScore Compute(IReadOnlyCollection<ExamSectionInput> sections, decimal passThresholdPercent)
    {
        var totalMax = sections.Sum(s => s.MaxScore);
        if (totalMax <= 0m) return new ExamScore(0m, false);
        var totalScore = sections.Sum(s => s.Score);
        var pct = Math.Round(totalScore / totalMax * 100m, 2, MidpointRounding.AwayFromZero);
        return new ExamScore(pct, pct >= passThresholdPercent);
    }
}

/// <summary>An existing persisted section (id + its current max), for the change check.</summary>
public sealed record ExamSectionState(Guid Id, decimal MaxScore);

/// <summary>A requested section in an exam-config update. Id null = a brand-new section.</summary>
public sealed record ExamSectionEdit(Guid? Id, decimal MaxScore);

/// <summary>
/// Pure rule for F8: once any result exists, DESTRUCTIVE section edits are blocked —
/// removing a section or decreasing its max would destroy entered per-section data
/// and shift overall %. Adding sections, renaming, and increasing a max are allowed.
/// </summary>
public static class ExamSectionChange
{
    public static bool IsDestructive(
        IReadOnlyCollection<ExamSectionState> existing,
        IReadOnlyCollection<ExamSectionEdit> requested)
    {
        var requestedMaxById = requested
            .Where(r => r.Id is not null)
            .ToDictionary(r => r.Id!.Value, r => r.MaxScore);

        foreach (var ex in existing)
        {
            if (!requestedMaxById.TryGetValue(ex.Id, out var newMax)) return true; // section removed
            if (newMax < ex.MaxScore) return true; // max decreased
        }
        return false;
    }
}
