using System.Text.Json;
using FluentAssertions;
using LMS.Application.Features.Exercises;
using Xunit;

namespace LMS.Tests;

/// <summary>
/// Pure-core coverage for <see cref="ExerciseChecker"/> — the self-check scorer behind
/// the lesson-exercise system. Verifies each supported type, the two accepted user-answer
/// shapes (object-by-id / array-by-order), case/space-insensitive matching, multi-gap and
/// dialogue scoring, and the graceful fall-through for unknown types + missing answers.
/// </summary>
public sealed class ExerciseCheckerTests
{
    private static JsonElement El(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void Mcq_scores_each_item_by_id()
    {
        var content = """{"items":[{"id":"1","answer":"a"},{"id":"2","answer":"b"}]}""";
        var (score, total) = ExerciseChecker.Check("mcq", content, El("""{"1":"a","2":"x"}"""));
        score.Should().Be(1);
        total.Should().Be(2);
    }

    [Fact]
    public void Items_also_accept_answers_by_array_order()
    {
        var content = """{"items":[{"id":"1","answer":"is"},{"id":"2","answer":"are"}]}""";
        var (score, total) = ExerciseChecker.Check("fill_blank", content, El("""["is","are"]"""));
        score.Should().Be(2);
        total.Should().Be(2);
    }

    [Fact]
    public void Matching_is_trimmed_and_case_insensitive()
    {
        var content = """{"items":[{"id":"1","answer":"London"}]}""";
        var (score, total) = ExerciseChecker.Check("error_correction", content, El("""{"1":"  london "}"""));
        score.Should().Be(1);
        total.Should().Be(1);
    }

    [Fact]
    public void Paragraph_cloze_compares_answers_by_index()
    {
        var content = """{"answers":["the","a","an"]}""";
        var (score, total) = ExerciseChecker.Check("paragraph_cloze", content, El("""["the","a","the"]"""));
        score.Should().Be(2);
        total.Should().Be(3);
    }

    [Fact]
    public void Word_bank_gap_handles_multi_gap_items()
    {
        var content = """{"items":[{"id":"1","answers":["go","went"]},{"id":"2","answer":"see"}]}""";
        var (score, total) = ExerciseChecker.Check("word_bank_gap", content, El("""{"1":["go","wrong"],"2":"see"}"""));
        score.Should().Be(2);  // "go" + "see"
        total.Should().Be(3);  // 2 gaps in item 1 + 1 in item 2
    }

    [Fact]
    public void Dialogue_compares_each_items_answer_lines()
    {
        var content = """{"items":[{"id":"1","answers":["hello","bye"]}]}""";
        var (score, total) = ExerciseChecker.Check("dialogue", content, El("""{"1":["hello","later"]}"""));
        score.Should().Be(1);
        total.Should().Be(2);
    }

    [Fact]
    public void Unknown_type_is_ungraded_not_a_crash()
    {
        var (score, total) = ExerciseChecker.Check("totally_new_type", """{"items":[]}""", El("{}"));
        score.Should().Be(0);
        total.Should().Be(0);
    }

    [Fact]
    public void Missing_user_answer_counts_as_wrong_not_error()
    {
        var content = """{"items":[{"id":"1","answer":"a"},{"id":"2","answer":"b"}]}""";
        var (score, total) = ExerciseChecker.Check("mcq", content, El("""{"1":"a"}"""));
        score.Should().Be(1);
        total.Should().Be(2);
    }

    [Fact]
    public void Word_completion_scores_like_a_single_answer_item()
    {
        var content = """{"items":[{"id":"1","text":"r_ng","answer":"ring"},{"id":"2","text":"c_m_r_","answer":"camera"}]}""";
        var (score, total) = ExerciseChecker.Check("word_completion", content, El("""{"1":"ring","2":"camra"}"""));
        score.Should().Be(1);
        total.Should().Be(2);
    }

    [Fact]
    public void Matching_compares_the_selected_key_against_the_answer_key()
    {
        var content = """{"items":[{"id":"1","text":"What's your name?","answer":"a"},{"id":"2","text":"Are you a student?","answer":"b"}],"options":{"a":"Pat.","b":"Yes."}}""";
        var (score, total) = ExerciseChecker.Check("matching", content, El("""{"1":"a","2":"a"}"""));
        score.Should().Be(1);
        total.Should().Be(2);
    }

    [Fact]
    public void True_false_scores_like_a_single_answer_item()
    {
        var content = """{"items":[{"id":"1","text":"Today is Carla's first day.","answer":"True"},{"id":"2","text":"Rob is Spanish.","answer":"False"}]}""";
        var (score, total) = ExerciseChecker.Check("true_false", content, El("""{"1":"True","2":"True"}"""));
        score.Should().Be(1);
        total.Should().Be(2);
    }

    [Fact]
    public void Mcq_with_gaps_scores_each_blank_independently()
    {
        // "A: What time ___ now? B: It's ___ past three." — two choice blanks.
        var content = """
        {"items":[{"id":"q1","parts":["A: What time "," now? B: It's "," past three."],
          "gaps":[{"options":["is it","it is"],"answer":"is it"},
                  {"options":["half","a half"],"answer":"a half"}]}]}
        """;
        var (score, total) = ExerciseChecker.Check("mcq", content, El("""{"q1":["is it","half"]}"""));
        score.Should().Be(1); // first gap right, second wrong
        total.Should().Be(2);
    }

    [Fact]
    public void Multi_select_rewards_correct_ticks_and_penalises_wrong_ones()
    {
        var content = """{"choices":["Italy","Spain","Russia","Japan"],"answers":["Spain","Russia"]}""";
        // Two correct + one wrong → 2 correct − 1 wrong = 1 / 2.
        var (score, total) = ExerciseChecker.Check("multi_select", content, El("""["Spain","Russia","Japan"]"""));
        score.Should().Be(1);
        total.Should().Be(2);
        // Exactly right → full marks.
        ExerciseChecker.Check("multi_select", content, El("""["Spain","Russia"]""")).Should().Be((2, 2));
    }

    [Fact]
    public void Crossword_scores_each_entry_from_its_filled_cells()
    {
        // 1-across CAFE at (0,1); 1-down CAR at (0,1) sharing the C.
        var content = """
        {"entries":[
          {"number":1,"direction":"across","clue":"coffee place","answer":"CAFE","row":0,"col":1},
          {"number":1,"direction":"down","clue":"you drive it","answer":"CAR","row":0,"col":1}]}
        """;
        // CAFE fully correct (case-insensitive); CAR has a wrong last cell (2,1).
        var user = El("""{"0,1":"c","0,2":"a","0,3":"f","0,4":"e","1,1":"a","2,1":"x"}""");
        var (score, total) = ExerciseChecker.Check("crossword", content, user);
        score.Should().Be(1);
        total.Should().Be(2);
    }

    [Fact]
    public void Word_search_scores_by_words_found_case_insensitively()
    {
        var content = """{"words":["DOCTOR","NURSE","PILOT"],"grid":[["x"]]}""";
        // Found two real words (case-insensitive) + one bogus that isn't counted.
        var (score, total) = ExerciseChecker.Check("word_search", content, El("""["doctor","pilot","zzz"]"""));
        score.Should().Be(2);
        total.Should().Be(3);
    }

    [Fact]
    public void Table_fill_scores_only_the_blank_cells_by_coordinate()
    {
        // Row 0: given name/city + three blanks (country, nationality, job).
        var content = """
        {"columns":["Name","City","Country","Nationality","Job"],
         "rows":[{"cells":[
            {"text":"Sara Demir"},{"text":"Istanbul"},
            {"answer":"Turkey"},{"answer":"Turkish"},{"answer":"doctor"}]}]}
        """;
        // Two right (0,2 case-insensitive + 0,4), one wrong (0,3); given cells aren't graded.
        var (score, total) = ExerciseChecker.Check(
            "table_fill", content, El("""{"0,2":" turkey ","0,3":"French","0,4":"doctor"}"""));
        score.Should().Be(2);
        total.Should().Be(3);
    }

    [Fact]
    public void Writing_is_completed_when_enough_words_are_written()
    {
        var content = """{"instructions":"Write about your day.","minWords":5}""";

        // Under the floor → not complete.
        ExerciseChecker.Check("writing", content, El("""{"text":"Too short"}""")).Should().Be((0, 1));
        // Meets the floor → complete (writing is never marked right/wrong).
        ExerciseChecker.Check("writing", content, El("""{"text":"I woke up and had a big breakfast."}""")).Should().Be((1, 1));
        // Empty → not complete.
        ExerciseChecker.Check("writing", content, El("""{"text":"   "}""")).Should().Be((0, 1));
        // No minWords → any non-empty text completes it.
        ExerciseChecker.Check("writing", """{"instructions":"Free write."}""", El("""{"text":"Hello"}""")).Should().Be((1, 1));
    }
}
