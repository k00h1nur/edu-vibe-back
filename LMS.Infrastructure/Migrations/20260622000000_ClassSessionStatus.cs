using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// Schedule↔Curriculum: a teaching lifecycle status on each session
    /// (Planned / InProgress / Completed / Cancelled) + a completion timestamp.
    /// Additive + idempotent — Status defaults to 1 (Planned) so existing
    /// sessions keep their current meaning; CompletedAt is nullable.
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260622000000_ClassSessionStatus")]
    public partial class ClassSessionStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='class_sessions' AND column_name='Status') THEN
                        ALTER TABLE class_sessions ADD COLUMN ""Status"" integer NOT NULL DEFAULT 1;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='class_sessions' AND column_name='CompletedAt') THEN
                        ALTER TABLE class_sessions ADD COLUMN ""CompletedAt"" timestamp with time zone NULL;
                    END IF;
                END $$;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE class_sessions DROP COLUMN IF EXISTS ""Status"";
                ALTER TABLE class_sessions DROP COLUMN IF EXISTS ""CompletedAt"";
            ");
        }
    }
}
