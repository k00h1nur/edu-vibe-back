using System.Linq;
using System.Text.Json;

namespace LMS.Application.Features.Exercises;

/// <summary>
/// Self-check scoring. Compares a user's answers against the exercise content, branching
/// on the free-string <c>type</c>. Comparison is always trimmed + case-insensitive.
/// Returns (score, total): total = number of gradable slots, score = number matched.
///
/// EXTENSIBILITY CONTRACT: to support a new exercise type, add ONE new case to the
/// switch — no other code (entity, handler, controller, migration) changes.
///
/// User-answer shapes accepted (forgiving): an object keyed by item id
/// <c>{ "1": "is" }</c>, OR an array by item order <c>["is", ...]</c>; per-item values
/// may be a single string or an array (multi-gap / dialogue lines).
/// </summary>
public static class ExerciseChecker
{
    public static (int Score, int Total) Check(string type, string contentJson, JsonElement userAnswers)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(contentJson) ? "{}" : contentJson);
        var content = doc.RootElement;

        return type switch
        {
            "mcq" or "mcq_ab" or "fill_blank" or "error_correction" or "transform"
                or "word_completion" or "matching" or "true_false"
                => CheckItems(content, userAnswers, multiGap: false),
            "word_bank_gap"
                => CheckItems(content, userAnswers, multiGap: true),
            "multi_select"
                => CheckMultiSelect(content, userAnswers),
            "paragraph_cloze"
                => CheckByIndex(GetArray(content, "answers"), AsList(userAnswers)),
            "dialogue"
                => CheckDialogue(content, userAnswers),
            "word_search"
                => CheckWordSearch(content, userAnswers),
            _ => (0, 0), // unknown type → ungraded (no crash); add a case to support it.
        };
    }

    // ---- per-type strategies -------------------------------------------------

    /// <summary>items[] each with a single "answer" (or, when multiGap, an "answers" array
    /// = one expected per gap in that item).</summary>
    private static (int, int) CheckItems(JsonElement content, JsonElement userAnswers, bool multiGap)
    {
        int score = 0, total = 0;
        if (!content.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            return (0, 0);

        var i = 0;
        foreach (var item in items.EnumerateArray())
        {
            var id = item.TryGetProperty("id", out var idEl) ? idEl.ToString() : i.ToString();
            var userForItem = UserAnswerFor(userAnswers, id, i);

            // Multi-gap choice item: one option-set per blank, answer per gap.
            if (item.TryGetProperty("gaps", out var gaps) && gaps.ValueKind == JsonValueKind.Array)
            {
                var expectedGaps = new List<string>();
                foreach (var gap in gaps.EnumerateArray())
                    expectedGaps.Add(gap.TryGetProperty("answer", out var ga) ? ga.ToString() : string.Empty);
                var (s, t) = CheckByIndex(expectedGaps, AsList(userForItem));
                score += s;
                total += t;
            }
            else if (multiGap && item.TryGetProperty("answers", out var ans) && ans.ValueKind == JsonValueKind.Array)
            {
                var (s, t) = CheckByIndex(AsList(ans), AsList(userForItem));
                score += s;
                total += t;
            }
            else
            {
                total++;
                var expected = item.TryGetProperty("answer", out var a) ? a.ToString() : null;
                if (expected is not null && Norm(Single(userForItem)) == Norm(expected)) score++;
            }
            i++;
        }
        return (score, total);
    }

    /// <summary>Aligned compare of two string lists by index. total = expected.Count.</summary>
    private static (int, int) CheckByIndex(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
    {
        var score = 0;
        for (var i = 0; i < expected.Count; i++)
            if (i < actual.Count && Norm(actual[i]) == Norm(expected[i])) score++;
        return (score, expected.Count);
    }

    /// <summary>items[] each with an "answers" array; the user's answer for that item is an array.</summary>
    private static (int, int) CheckDialogue(JsonElement content, JsonElement userAnswers)
    {
        int score = 0, total = 0;
        if (!content.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            return (0, 0);

        var i = 0;
        foreach (var item in items.EnumerateArray())
        {
            var id = item.TryGetProperty("id", out var idEl) ? idEl.ToString() : i.ToString();
            var (s, t) = CheckByIndex(GetArray(item, "answers"), AsList(UserAnswerFor(userAnswers, id, i)));
            score += s;
            total += t;
            i++;
        }
        return (score, total);
    }

    /// <summary>"Tick all that apply": compare the user's ticked set against content.answers.
    /// total = number of correct answers; score = correctly ticked minus wrongly ticked (floored).</summary>
    private static (int, int) CheckMultiSelect(JsonElement content, JsonElement userAnswers)
    {
        var correct = GetArray(content, "answers").Select(Norm).ToHashSet();
        if (correct.Count == 0) return (0, 0);
        var ticked = AsList(userAnswers).Select(Norm).Where(s => s.Length > 0).ToHashSet();
        var correctTicked = ticked.Count(t => correct.Contains(t));
        var wrongTicked = ticked.Count - correctTicked;
        return (Math.Max(0, correctTicked - wrongTicked), correct.Count);
    }

    /// <summary>Word search: content.words = the words to find; the user submits the array of
    /// words they located. total = distinct words; score = how many were found (case-insensitive).
    /// The grid + placements are for rendering only — scoring is by word, matching the self-check
    /// model (answers ship in the content).</summary>
    private static (int, int) CheckWordSearch(JsonElement content, JsonElement userAnswers)
    {
        var words = GetArray(content, "words").Select(Norm).Where(s => s.Length > 0).Distinct().ToList();
        if (words.Count == 0) return (0, 0);
        var found = AsList(userAnswers).Select(Norm).ToHashSet();
        return (words.Count(w => found.Contains(w)), words.Count);
    }

    // ---- helpers -------------------------------------------------------------

    private static string Norm(string? s) => (s ?? string.Empty).Trim().ToLowerInvariant();

    private static string? Single(JsonElement? el) => el is not { } e
        ? null
        : e.ValueKind switch
        {
            JsonValueKind.String => e.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => e.ToString(),
            _ => null,
        };

    private static List<string> AsList(JsonElement? el)
    {
        var list = new List<string>();
        if (el is { ValueKind: JsonValueKind.Array } arr)
            foreach (var x in arr.EnumerateArray()) list.Add(Single(x) ?? string.Empty);
        else if (Single(el) is { } single)
            list.Add(single);
        return list;
    }

    private static List<string> GetArray(JsonElement obj, string prop)
        => obj.TryGetProperty(prop, out var arr) ? AsList(arr) : new List<string>();

    /// <summary>The user's answer for an item — by id (object) or by order (array).</summary>
    private static JsonElement? UserAnswerFor(JsonElement userAnswers, string id, int index)
    {
        if (userAnswers.ValueKind == JsonValueKind.Object)
            return userAnswers.TryGetProperty(id, out var byId) ? byId : null;
        if (userAnswers.ValueKind == JsonValueKind.Array && index >= 0 && index < userAnswers.GetArrayLength())
            return userAnswers[index];
        return null;
    }
}
