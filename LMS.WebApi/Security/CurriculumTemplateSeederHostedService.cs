using LMS.Application.Common.Abstractions;
using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LMS.WebApi.Security;

/// <summary>
/// Seeds the built-in curriculum template library — the catalogue teachers clone
/// from when they "start from a template".
///
/// Idempotent and ADD-ONLY (F3 "reconcile-to-spec"):
///  • A system template whose name doesn't exist yet is created in full.
///  • A system template that already exists is reconciled — for every unit in the
///    spec, any lesson whose Order is missing is added. Existing rows are never
///    edited or removed, so admin content and class clones stay untouched, and a
///    re-run on an existing DB safely tops up the new lessons without duplicating.
///
/// Beginner English + Elementary English carry exactly 6 lessons per unit; the
/// other templates keep their original (smaller) shape — reconciling them against
/// their unchanged specs is a no-op.
/// </summary>
public sealed class CurriculumTemplateSeederHostedService(
    IServiceProvider serviceProvider,
    ILogger<CurriculumTemplateSeederHostedService> logger)
    : IHostedService
{
    private sealed record L(string Title, string? Objectives, string? Homework, string? Materials, bool Exam = false);
    private sealed record U(string Title, params L[] Lessons);
    private sealed record M(string Title, params U[] Units);
    private sealed record T(string Name, CurriculumCategory Category, string? Level, string Description, params M[] Modules);

    private static readonly T[] Library =
    {
        // Beginner — 1 module, 3 units × 6 lessons. The first 1–2 lessons of each
        // unit match the original seed (same Order) so reconcile only appends.
        new("Beginner English", CurriculumCategory.GeneralEnglish, "A1",
            "Starter general-English path covering greetings, family and daily life.",
            new M("Module 1: Foundations",
                new U("Greetings",
                    new L("Introducing Yourself", "Say your name, age and nationality\nAsk someone's name",
                        "Write a 3-sentence self-introduction", "Greetings slide deck"),
                    new L("Formal & Informal Greetings", "Choose the right greeting for the situation",
                        "Match greetings to situations worksheet", "Greetings reference sheet"),
                    new L("The Alphabet & Spelling", "Spell your name aloud; ask \"How do you spell…?\"",
                        "Spell five words out loud and write them", "Alphabet chart"),
                    new L("Numbers 0–20", "Count to twenty and say phone numbers",
                        "Write the numbers 0–20 in words", "Number flashcards"),
                    new L("Countries & Nationalities", "Say where you and others are from",
                        "Match ten countries to nationalities", "World map handout"),
                    new L("Greetings Review", "Consolidate greetings, spelling and personal info",
                        "Greetings & introductions quiz", "Review sheet", Exam: true)),
                new U("Family",
                    new L("Family Members", "Name immediate family members\nDescribe your family",
                        "Draw + label your family tree", "Family vocabulary cards"),
                    new L("Possessive Adjectives", "Use my/your/his/her/our/their correctly",
                        "Gap-fill: possessive adjectives", "Grammar handout"),
                    new L("Possessive 's", "Show possession with apostrophe-s",
                        "Rewrite five sentences using 's", "Grammar handout"),
                    new L("Describing People", "Describe age and basic appearance",
                        "Write three sentences about a relative", "Adjective list"),
                    new L("This / That / These / Those", "Point out people and objects",
                        "Gap-fill: demonstratives", "Demonstratives slides"),
                    new L("Family Review", "Consolidate family vocabulary and possession",
                        "Family unit quiz", "Review sheet", Exam: true)),
                new U("Daily Life",
                    new L("Daily Routine", "Describe a typical day using time expressions",
                        "Write your daily routine (6 sentences)", "Daily-routine picture set"),
                    new L("Present Simple", "Form affirmative/negative/question in present simple",
                        "Present-simple quiz", "Present-simple slides", Exam: true),
                    new L("Telling the Time", "Ask for and tell the time",
                        "Clock-reading worksheet", "Clock cards"),
                    new L("Days & Months", "Say the days, months and dates",
                        "Write five important dates", "Calendar handout"),
                    new L("Frequency Adverbs", "Use always/usually/sometimes/never",
                        "Write about your week with frequency adverbs", "Frequency slides"),
                    new L("Daily Life Review", "Consolidate routine, time and present simple",
                        "Mixed daily-life quiz", "Review sheet", Exam: true)))),

        // Elementary — 2 modules, 8 units × 6 lessons. First lesson of each unit
        // matches the original seed; reconcile appends the remaining five.
        new("Elementary English", CurriculumCategory.GeneralEnglish, "A1",
            "Elementary general-English path — everyday situations and core grammar.",
            new M("Module 1: Everyday Life",
                new U("Greetings",
                    new L("Meeting People", "Greet and introduce people", "Role-play script", "Slides"),
                    new L("Greetings & Goodbyes", "Open and close a conversation politely", "Greetings dialogue", "Phrase list"),
                    new L("Personal Information", "Exchange name, age, job and address", "Complete a profile form", "Form template"),
                    new L("The Verb 'to be'", "Use am/is/are in statements and questions", "Gap-fill: to be", "Grammar handout"),
                    new L("Question Words", "Ask with who/what/where/when/how", "Write five questions", "Question-word slides"),
                    new L("Greetings Review", "Consolidate introductions and 'to be'", "Unit quiz", "Review sheet", Exam: true)),
                new U("Family",
                    new L("My Family", "Talk about your family", "Family description", "Vocab cards"),
                    new L("Possessive Adjectives", "Use my/your/his/her correctly", "Gap-fill: possessives", "Grammar handout"),
                    new L("Describing Family", "Describe age, job and looks", "Describe three relatives", "Adjective list"),
                    new L("Jobs & Occupations", "Name common jobs and workplaces", "Match jobs to places", "Jobs flashcards"),
                    new L("Have got", "Talk about family and possessions with have got", "Gap-fill: have got", "Grammar slides"),
                    new L("Family Review", "Consolidate family and have got", "Unit quiz", "Review sheet", Exam: true)),
                new U("Numbers",
                    new L("Numbers & Prices", "Use numbers 1–100 and prices", "Shopping worksheet", "Number flashcards"),
                    new L("Big Numbers", "Say hundreds, thousands and years", "Write ten big numbers in words", "Number chart"),
                    new L("Ordinal Numbers", "Use first, second, third… for dates", "Order a list of dates", "Ordinals handout"),
                    new L("Money & Currency", "Talk about money and make change", "Price role-play", "Currency images"),
                    new L("Quantities", "Use how much / how many", "Gap-fill: much/many", "Quantity slides"),
                    new L("Numbers Review", "Consolidate numbers, prices and quantities", "Unit quiz", "Review sheet", Exam: true)),
                new U("Daily Routine",
                    new L("A Typical Day", "Describe routines with time", "Routine paragraph", "Picture set"),
                    new L("Present Simple", "Form present simple for habits", "Present-simple worksheet", "Grammar slides"),
                    new L("Frequency Adverbs", "Say how often you do things", "Write about your week", "Frequency slides"),
                    new L("Telling the Time", "Ask and tell the time precisely", "Clock worksheet", "Clock cards"),
                    new L("Weekends & Free Time", "Talk about leisure activities", "Describe your weekend", "Hobby images"),
                    new L("Routine Review", "Consolidate routine and present simple", "Unit quiz", "Review sheet", Exam: true))),
            new M("Module 2: Out & About",
                new U("Food",
                    new L("At the Restaurant", "Order food and drinks", "Menu role-play", "Menu handout"),
                    new L("Food & Drink Vocabulary", "Name everyday food and drink", "Sort food into groups", "Food flashcards"),
                    new L("Countable & Uncountable", "Tell countable from uncountable nouns", "Sort nouns worksheet", "Grammar handout"),
                    new L("some / any", "Use some and any with food", "Gap-fill: some/any", "Grammar slides"),
                    new L("Likes & Dislikes", "Say what you like and dislike", "Write about your tastes", "Opinion phrases"),
                    new L("Food Review", "Consolidate food vocabulary and quantifiers", "Unit quiz", "Review sheet", Exam: true)),
                new U("Shopping",
                    new L("Going Shopping", "Ask for items and prices", "Shopping dialogue", "Store images"),
                    new L("Clothes & Sizes", "Name clothes and ask for sizes", "Label a clothes picture", "Clothes flashcards"),
                    new L("In the Shop", "Handle a simple purchase", "Shop role-play", "Dialogue cards"),
                    new L("this / that / these / those", "Point out items in a shop", "Gap-fill: demonstratives", "Grammar slides"),
                    new L("Comparatives", "Compare two products (cheaper, bigger)", "Compare five item pairs", "Comparatives handout"),
                    new L("Shopping Review", "Consolidate shopping language", "Unit quiz", "Review sheet", Exam: true)),
                new U("Time",
                    new L("Telling the Time", "Tell and ask the time", "Clock worksheet", "Clock cards"),
                    new L("Days & Dates", "Say days, months and dates", "Write key dates", "Calendar handout"),
                    new L("Prepositions of Time", "Use in/on/at with time", "Gap-fill: in/on/at", "Grammar slides"),
                    new L("Daily Schedule", "Describe a timetable", "Write your weekly schedule", "Timetable template"),
                    new L("Making Appointments", "Arrange a time to meet", "Appointment role-play", "Dialogue cards"),
                    new L("Time Review", "Consolidate time language", "Unit quiz", "Review sheet", Exam: true)),
                new U("Directions",
                    new L("Asking for Directions", "Give and follow directions", "Map task", "City map", Exam: true),
                    new L("Places in Town", "Name shops and public places", "Label a town map", "Town flashcards"),
                    new L("Prepositions of Place", "Use next to / opposite / between", "Gap-fill: place prepositions", "Grammar slides"),
                    new L("Giving Directions", "Direct someone step by step", "Write directions to your home", "Map handout"),
                    new L("Public Transport", "Buy a ticket and ask about transport", "Transport role-play", "Transport images"),
                    new L("Directions Review", "Consolidate directions and places", "Unit quiz", "Review sheet", Exam: true)))),

        new("Pre-Intermediate", CurriculumCategory.GeneralEnglish, "A2",
            "Pre-intermediate path — past/future tenses and richer everyday topics.",
            new M("Module 1: Talking About Time",
                new U("Past Simple", new L("Last Weekend", "Talk about past events", "Past-simple story", "Verb list")),
                new U("Future Plans", new L("Going To & Will", "Talk about plans and predictions", "Plans paragraph", "Slides")),
                new U("Travel", new L("Holidays", "Describe a trip", "Holiday postcard", "Travel images")),
                new U("Review", new L("Progress Check", "Consolidate tenses", "Mixed-tense quiz", "Review sheet", Exam: true)))),

        new("IELTS Foundation", CurriculumCategory.Ielts, "B1",
            "IELTS foundation — task familiarisation across all four skills.",
            new M("Module 1: Skills Foundation",
                new U("Listening", new L("Section 1 Strategies", "Predict and follow form-completion tasks", "Practice Section 1", "Audio set")),
                new U("Reading", new L("Skimming & Scanning", "Locate information quickly", "Reading passage practice", "Sample passages")),
                new U("Writing", new L("Task 1 Overview", "Describe a simple chart", "Write a 150-word report", "Sample charts")),
                new U("Speaking", new L("Part 1 Practice", "Answer familiar-topic questions", "Record a Part 1 answer", "Question bank", Exam: true)))),

        new("CEFR A1", CurriculumCategory.Cefr, "A1",
            "CEFR A1 can-do path — the breakthrough level descriptors.",
            new M("Module 1: A1 Can-Do",
                new U("Personal Information", new L("About Me", "Give basic personal information", "Profile card", "Form template")),
                new U("Everyday Expressions", new L("Survival Phrases", "Use everyday polite expressions", "Phrase list task", "Phrasebook")),
                new U("Simple Interactions", new L("Asking & Answering", "Ask and answer simple questions", "Pair dialogue", "Prompt cards", Exam: true)))),
    };

    public async Task StartAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        foreach (var spec in Library)
            await ReconcileTemplateAsync(db, spec, ct);
    }

    private async Task ReconcileTemplateAsync(IApplicationDbContext db, T spec, CancellationToken ct)
    {
        var template = await db.CurriculumTemplates
            .Include(t => t.Modules).ThenInclude(m => m.Units).ThenInclude(u => u.Lessons)
            .FirstOrDefaultAsync(t => t.IsSystem && t.Name == spec.Name, ct);

        if (template is null)
        {
            await CreateTemplateAsync(db, spec, ct);
            logger.LogInformation("Seeded curriculum template '{Name}'.", spec.Name);
            return;
        }

        // Exists — top up any lesson whose Order is missing. Add-only: never edits
        // or removes existing rows. Modules/units are matched by Order; if the
        // stored structure has diverged from the spec, the divergent node is left
        // alone rather than guessed at.
        var added = 0;
        var mOrder = 1;
        foreach (var m in spec.Modules)
        {
            var module = template.Modules.FirstOrDefault(x => x.Order == mOrder++);
            if (module is null) continue;

            var uOrder = 1;
            foreach (var u in m.Units)
            {
                var unit = module.Units.FirstOrDefault(x => x.Order == uOrder++);
                if (unit is null) continue;

                var existingOrders = unit.Lessons.Select(x => x.Order).ToHashSet();
                var lOrder = 1;
                foreach (var l in u.Lessons)
                {
                    if (!existingOrders.Contains(lOrder))
                    {
                        await db.CurriculumLessons.AddAsync(
                            new CurriculumLesson(unit.Id, lOrder, l.Title, l.Objectives, l.Homework, l.Materials, l.Exam), ct);
                        added++;
                    }
                    lOrder++;
                }
            }
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Reconciled curriculum template '{Name}': added {Count} missing lesson(s).", spec.Name, added);
        }
    }

    private static async Task CreateTemplateAsync(IApplicationDbContext db, T spec, CancellationToken ct)
    {
        var template = new CurriculumTemplate(spec.Name, spec.Category, spec.Level, spec.Description, isSystem: true);
        await db.CurriculumTemplates.AddAsync(template, ct);
        await db.SaveChangesAsync(ct);

        var mOrder = 1;
        foreach (var m in spec.Modules)
        {
            var module = new CurriculumModule(template.Id, mOrder++, m.Title);
            await db.CurriculumModules.AddAsync(module, ct);
            await db.SaveChangesAsync(ct);

            var uOrder = 1;
            foreach (var u in m.Units)
            {
                var unit = new CurriculumUnit(module.Id, uOrder++, u.Title);
                await db.CurriculumUnits.AddAsync(unit, ct);
                await db.SaveChangesAsync(ct);

                var lOrder = 1;
                foreach (var l in u.Lessons)
                    await db.CurriculumLessons.AddAsync(
                        new CurriculumLesson(unit.Id, lOrder++, l.Title, l.Objectives, l.Homework, l.Materials, l.Exam), ct);
                await db.SaveChangesAsync(ct);
            }
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
