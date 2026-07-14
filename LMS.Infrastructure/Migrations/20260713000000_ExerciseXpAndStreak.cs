using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// Game rewards for self-check exercises. Two additive columns:
    ///   • lesson_exercise_submissions.XpAwarded (bool, default false) — guards the
    ///     one-time XP grant for a perfect completion (sticky across re-submits);
    ///   • student_profiles.LastActivityOn (date, nullable) — drives the consecutive-day
    ///     streak computed in StudentProfile.RegisterDailyActivity.
    ///
    /// House style: hand-authored + idempotent (ADD COLUMN IF NOT EXISTS). Re-running on a
    /// populated DB is a safe no-op; existing rows default to XpAwarded=false / null date,
    /// so no past completion is retroactively rewarded.
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260713000000_ExerciseXpAndStreak")]
    public partial class ExerciseXpAndStreak : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE lesson_exercise_submissions
                    ADD COLUMN IF NOT EXISTS ""XpAwarded"" boolean NOT NULL DEFAULT false;

                ALTER TABLE student_profiles
                    ADD COLUMN IF NOT EXISTS ""LastActivityOn"" date NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE lesson_exercise_submissions
                    DROP COLUMN IF EXISTS ""XpAwarded"";

                ALTER TABLE student_profiles
                    DROP COLUMN IF EXISTS ""LastActivityOn"";
            ");
        }
    }
}
