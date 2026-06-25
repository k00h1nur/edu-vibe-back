using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// F6 — marks a session as synthetic backfill (existing-group onboarding).
    /// Additive + idempotent: adds class_sessions."IsBackfilled" (default false).
    /// Session-count analytics exclude these; the student roadmap still counts them.
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260629000000_ClassSessionIsBackfilled")]
    public partial class ClassSessionIsBackfilled : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE class_sessions
                    ADD COLUMN IF NOT EXISTS ""IsBackfilled"" boolean NOT NULL DEFAULT false;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE class_sessions DROP COLUMN IF EXISTS ""IsBackfilled"";");
        }
    }
}
