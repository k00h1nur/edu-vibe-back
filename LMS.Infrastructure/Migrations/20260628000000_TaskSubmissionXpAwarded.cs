using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// F4 — XP-on-grade idempotency flag. Additive + idempotent: adds
    /// task_submissions."XpAwarded" (default false) so XP is granted at most once
    /// per submission across re-submits and re-grades.
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260628000000_TaskSubmissionXpAwarded")]
    public partial class TaskSubmissionXpAwarded : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE task_submissions
                    ADD COLUMN IF NOT EXISTS ""XpAwarded"" boolean NOT NULL DEFAULT false;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE task_submissions DROP COLUMN IF EXISTS ""XpAwarded"";");
        }
    }
}
