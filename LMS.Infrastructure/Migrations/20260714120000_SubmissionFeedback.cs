using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// Richer grading for assignment submissions (used by the Reading/Listening/Writing
    /// skill-task flow). Adds two nullable columns to submissions:
    ///   • MaxScore — numeric(10,2): the scale graded on (e.g. 10 for "8/10", or a band);
    ///   • Feedback — character varying(4000): the teacher's written comment.
    ///
    /// House style: hand-authored + idempotent (ADD COLUMN IF NOT EXISTS). Existing
    /// graded rows keep their raw Score; MaxScore/Feedback default null.
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260714120000_SubmissionFeedback")]
    public partial class SubmissionFeedback : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE submissions
                    ADD COLUMN IF NOT EXISTS ""MaxScore"" numeric(10,2) NULL,
                    ADD COLUMN IF NOT EXISTS ""Feedback"" character varying(4000) NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE submissions
                    DROP COLUMN IF EXISTS ""MaxScore"",
                    DROP COLUMN IF EXISTS ""Feedback"";
            ");
        }
    }
}
