using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// Teacher grading for open-ended lesson exercises (e.g. writing), which auto-check to
    /// 0/0. Adds nullable columns to lesson_exercise_submissions:
    ///   • TeacherScore / TeacherMaxScore — numeric(6,2) so bands like 6.5 fit;
    ///   • TeacherFeedback — the comment the student sees;
    ///   • GradedByUserId / GradedAt — who graded, when.
    ///
    /// House style: hand-authored + idempotent (ADD COLUMN IF NOT EXISTS). Re-running on a
    /// populated DB is a safe no-op; existing self-check results are untouched.
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260709000000_WritingTeacherGrading")]
    public partial class WritingTeacherGrading : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE lesson_exercise_submissions
                    ADD COLUMN IF NOT EXISTS ""TeacherScore""    numeric(6,2) NULL,
                    ADD COLUMN IF NOT EXISTS ""TeacherMaxScore"" numeric(6,2) NULL,
                    ADD COLUMN IF NOT EXISTS ""TeacherFeedback"" character varying(4000) NULL,
                    ADD COLUMN IF NOT EXISTS ""GradedByUserId""  uuid NULL,
                    ADD COLUMN IF NOT EXISTS ""GradedAt""        timestamp with time zone NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE lesson_exercise_submissions
                    DROP COLUMN IF EXISTS ""TeacherScore"",
                    DROP COLUMN IF EXISTS ""TeacherMaxScore"",
                    DROP COLUMN IF EXISTS ""TeacherFeedback"",
                    DROP COLUMN IF EXISTS ""GradedByUserId"",
                    DROP COLUMN IF EXISTS ""GradedAt"";
            ");
        }
    }
}
