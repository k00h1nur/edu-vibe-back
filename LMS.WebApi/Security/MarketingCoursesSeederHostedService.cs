using LMS.Application.Common.Abstractions;
using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LMS.WebApi.Security;

/// <summary>
/// Seeds the marketing course catalog into <c>marketing_courses</c> the first
/// time the table is empty, so the public Courses page shows real, admin-owned
/// data instead of the hard-coded static fallback. Idempotent: once any course
/// exists (seeded here or created via admin) this never runs again, so admin
/// edits/removals are preserved.
///
/// Note: marketing_courses is single-language (English). The marketing site's
/// static catalog carries uz/ru/en; once this CMS list is populated the site
/// treats it as the source of truth (English). Disable with
/// <c>MarketingCourses:SeedCatalog=false</c> to keep the multilingual fallback.
/// </summary>
public sealed class MarketingCoursesSeederHostedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<MarketingCoursesSeederHostedService> logger)
    : IHostedService
{
    private sealed record Seed(
        string Slug, string Title, string Subtitle, string Description,
        string DurationText, string LevelText, int SortOrder);

    // English projection of src/data/courses.ts. Popular tracks float to top.
    private static readonly Seed[] Catalog =
    {
        new("ielts", "IELTS", "Exam preparation",
            "Band 6.5 → 8+ prep with mock tests and individual writing feedback.",
            "3×/week • 180 min", "B1+ • 3 – 6 months", 1),
        new("sat", "SAT", "Exam preparation",
            "Math + Verbal score boost with timed drills and strategy work.",
            "3×/week • 90 min", "B2+ • 4 – 6 months", 2),
        new("general-english", "General English", "English",
            "Build everyday English fluency from A1 to B2 with a structured curriculum.",
            "3×/week • 90 min", "All ages • A1 → B2", 3),
        new("english-for-adults", "English for Adults", "English",
            "Practical English for work, travel and day-to-day communication.",
            "3×/week • 90 min", "Adults • A2 → C1", 4),
        new("speaking-club", "Speaking Club", "English",
            "Weekly conversation practice — fluency, accent and confidence.",
            "2×/week • 60 min", "All levels • Drop-in", 5),
        new("kids-english", "Kids English", "English",
            "Story-driven English for 6–12 year-olds with games and crafts.",
            "2×/week • 60 min", "Ages 6 – 12", 6),
        new("dtm", "DTM", "Exam preparation",
            "Uzbek national exam coaching with past-paper drills and timing.",
            "3×/week • 90 min", "Grade 11 • 6 – 9 months", 7),
        new("chinese", "Chinese", "Other languages",
            "Mandarin from scratch — characters, tones and real conversation.",
            "3×/week • 90 min", "All levels • HSK 1 → 4", 8),
        new("russian", "Russian", "Other languages",
            "Structured Russian for travel, study and daily life.",
            "3×/week • 90 min", "All levels • A1 → B2", 9),
        new("uzbek", "Uzbek", "Other languages",
            "Uzbek for foreign students, professionals and new arrivals.",
            "3×/week • 90 min", "All levels • A1 → C1", 10),
        new("math", "Math", "Math",
            "School math support from algebra through calculus and problem-solving.",
            "3×/week • 90 min", "Grades 5 – 11", 11),
    };

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue("MarketingCourses:SeedCatalog", true))
        {
            logger.LogDebug("Marketing course catalog seeding disabled by config.");
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        if (await db.MarketingCourses.AnyAsync(cancellationToken))
        {
            logger.LogDebug("marketing_courses already populated — skipping catalog seed.");
            return;
        }

        foreach (var s in Catalog)
        {
            var c = new MarketingCourse(
                s.Slug, s.Title, s.Subtitle, s.Description,
                coverImageUrl: null, priceText: null,
                durationText: s.DurationText, levelText: s.LevelText,
                sortOrder: s.SortOrder, isActive: true);
            await db.MarketingCourses.AddAsync(c, cancellationToken);
        }
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded {Count} marketing courses.", Catalog.Length);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
