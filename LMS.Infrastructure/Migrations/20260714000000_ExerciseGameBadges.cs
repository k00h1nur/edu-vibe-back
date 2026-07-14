using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// Seeds the three auto-awarded game badges (codes match GameBadges):
    ///   • GAME_FIRST     "First Steps"  (10 XP) — first XP-earning exercise;
    ///   • GAME_STREAK_7  "On Fire"      (50 XP) — 7-day practice streak;
    ///   • GAME_STREAK_30 "Unstoppable" (150 XP) — 30-day practice streak.
    ///
    /// House style: hand-authored + idempotent (INSERT … ON CONFLICT DO NOTHING with
    /// fixed ids, continuing the 40000000-… badge series). Re-running is a safe no-op.
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260714000000_ExerciseGameBadges")]
    public partial class ExerciseGameBadges : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO badges (""Id"", ""Name"", ""XpReward"", ""Code"", ""CreatedAt"", ""UpdatedAt"") VALUES
                    ('40000000-0000-0000-0000-000000000005', 'First Steps',  10,  'GAME_FIRST',     now(), now()),
                    ('40000000-0000-0000-0000-000000000006', 'On Fire',      50,  'GAME_STREAK_7',  now(), now()),
                    ('40000000-0000-0000-0000-000000000007', 'Unstoppable', 150,  'GAME_STREAK_30', now(), now())
                ON CONFLICT (""Id"") DO NOTHING;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM badges WHERE ""Id"" IN (
                    '40000000-0000-0000-0000-000000000005',
                    '40000000-0000-0000-0000-000000000006',
                    '40000000-0000-0000-0000-000000000007');
            ");
        }
    }
}
