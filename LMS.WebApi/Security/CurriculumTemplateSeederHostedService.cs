using LMS.Application.Common.Abstractions;
using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LMS.WebApi.Security;

/// <summary>
/// Seeds a built-in starter curriculum template the first time the platform runs
/// with the curriculum engine (idempotent: skipped once any template exists).
/// Phase 1 ships one reference template ("Beginner English") so the picker +
/// auto-generation are demonstrable end-to-end; the full default-content library
/// (Elementary / IELTS / SAT / CEFR …) lands in a later phase and clones from
/// these system templates.
/// </summary>
public sealed class CurriculumTemplateSeederHostedService(
    IServiceProvider serviceProvider,
    ILogger<CurriculumTemplateSeederHostedService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        if (await db.CurriculumTemplates.AnyAsync(cancellationToken)) return;

        var template = new CurriculumTemplate(
            "Beginner English", CurriculumCategory.GeneralEnglish, "A1",
            "Starter general-English path covering greetings, family and daily life.", isSystem: true);
        await db.CurriculumTemplates.AddAsync(template, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var module = new CurriculumModule(template.Id, 1, "Module 1: Foundations");
        await db.CurriculumModules.AddAsync(module, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var greetings = new CurriculumUnit(module.Id, 1, "Greetings");
        var family = new CurriculumUnit(module.Id, 2, "Family");
        var dailyLife = new CurriculumUnit(module.Id, 3, "Daily Life");
        await db.CurriculumUnits.AddRangeAsync(new[] { greetings, family, dailyLife }, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var lessons = new[]
        {
            new CurriculumLesson(greetings.Id, 1, "Introducing Yourself",
                "Say your name, age and nationality\nAsk someone's name", "Write a 3-sentence self-introduction",
                "Greetings slide deck", isAssessment: false),
            new CurriculumLesson(greetings.Id, 2, "Formal & Informal Greetings",
                "Choose the right greeting for the situation", "Match greetings to situations worksheet",
                "Greetings reference sheet", isAssessment: false),
            new CurriculumLesson(family.Id, 3, "Family Members",
                "Name immediate family members\nDescribe your family", "Draw + label your family tree",
                "Family vocabulary cards", isAssessment: false),
            new CurriculumLesson(family.Id, 4, "Possessive Adjectives",
                "Use my/your/his/her/our/their correctly", "Gap-fill: possessive adjectives",
                "Grammar handout", isAssessment: false),
            new CurriculumLesson(dailyLife.Id, 5, "Daily Routine",
                "Describe a typical day using time expressions", "Write your daily routine (6 sentences)",
                "Daily-routine picture set", isAssessment: false),
            new CurriculumLesson(dailyLife.Id, 6, "Present Simple",
                "Form affirmative/negative/question in present simple", "Present-simple quiz",
                "Present-simple slides", isAssessment: true),
        };
        await db.CurriculumLessons.AddRangeAsync(lessons, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded built-in curriculum template 'Beginner English' (1 module, 3 units, 6 lessons).");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
