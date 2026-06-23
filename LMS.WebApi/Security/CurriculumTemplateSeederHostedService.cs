using LMS.Application.Common.Abstractions;
using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LMS.WebApi.Security;

/// <summary>
/// Seeds the built-in curriculum template library — the catalogue teachers clone
/// from when they "start from a template" (no manual unit creation). Idempotent
/// PER TEMPLATE: each system template is added only if one with the same name
/// doesn't already exist, so the library grows on an existing database without
/// disturbing classes that already cloned from earlier templates.
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
        new("Beginner English", CurriculumCategory.GeneralEnglish, "A1",
            "Starter general-English path covering greetings, family and daily life.",
            new M("Module 1: Foundations",
                new U("Greetings",
                    new L("Introducing Yourself", "Say your name, age and nationality\nAsk someone's name",
                        "Write a 3-sentence self-introduction", "Greetings slide deck"),
                    new L("Formal & Informal Greetings", "Choose the right greeting for the situation",
                        "Match greetings to situations worksheet", "Greetings reference sheet")),
                new U("Family",
                    new L("Family Members", "Name immediate family members\nDescribe your family",
                        "Draw + label your family tree", "Family vocabulary cards"),
                    new L("Possessive Adjectives", "Use my/your/his/her/our/their correctly",
                        "Gap-fill: possessive adjectives", "Grammar handout")),
                new U("Daily Life",
                    new L("Daily Routine", "Describe a typical day using time expressions",
                        "Write your daily routine (6 sentences)", "Daily-routine picture set"),
                    new L("Present Simple", "Form affirmative/negative/question in present simple",
                        "Present-simple quiz", "Present-simple slides", Exam: true)))),

        new("Elementary English", CurriculumCategory.GeneralEnglish, "A1",
            "Elementary general-English path — everyday situations and core grammar.",
            new M("Module 1: Everyday Life",
                new U("Greetings", new L("Meeting People", "Greet and introduce people", "Role-play script", "Slides")),
                new U("Family", new L("My Family", "Talk about your family", "Family description", "Vocab cards")),
                new U("Numbers", new L("Numbers & Prices", "Use numbers 1–100 and prices", "Shopping worksheet", "Number flashcards")),
                new U("Daily Routine", new L("A Typical Day", "Describe routines with time", "Routine paragraph", "Picture set"))),
            new M("Module 2: Out & About",
                new U("Food", new L("At the Restaurant", "Order food and drinks", "Menu role-play", "Menu handout")),
                new U("Shopping", new L("Going Shopping", "Ask for items and prices", "Shopping dialogue", "Store images")),
                new U("Time", new L("Telling the Time", "Tell and ask the time", "Clock worksheet", "Clock cards")),
                new U("Directions", new L("Asking for Directions", "Give and follow directions", "Map task", "City map", Exam: true)))),

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

        var existing = (await db.CurriculumTemplates.AsNoTracking()
            .Where(t => t.IsSystem).Select(t => t.Name).ToListAsync(ct)).ToHashSet();

        foreach (var spec in Library)
        {
            if (existing.Contains(spec.Name)) continue;

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
            logger.LogInformation("Seeded curriculum template '{Name}'.", spec.Name);
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
