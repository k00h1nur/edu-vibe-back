using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// Lesson publishing + scheduled visibility on class_sessions:
    ///   • IsPublished  — whether the lesson content is shown to students.
    ///   • PublishedAt  — first-publish timestamp (info/audit).
    ///   • VisibleFrom  — hide content before this instant (null = no lower bound).
    ///   • VisibleUntil — hide content after this instant (null = no upper bound).
    /// Existing rows default to published=true so already-visible lessons stay
    /// visible. Raw idempotent SQL in house style; snapshot patched by hand.
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260618000000_LessonPublishing")]
    public partial class LessonPublishing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='class_sessions' AND column_name='IsPublished') THEN
                        ALTER TABLE class_sessions ADD COLUMN ""IsPublished"" boolean NOT NULL DEFAULT TRUE;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='class_sessions' AND column_name='PublishedAt') THEN
                        ALTER TABLE class_sessions ADD COLUMN ""PublishedAt"" timestamp with time zone NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='class_sessions' AND column_name='VisibleFrom') THEN
                        ALTER TABLE class_sessions ADD COLUMN ""VisibleFrom"" timestamp with time zone NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='class_sessions' AND column_name='VisibleUntil') THEN
                        ALTER TABLE class_sessions ADD COLUMN ""VisibleUntil"" timestamp with time zone NULL;
                    END IF;
                END $$;
                -- Stamp a publish time on existing visible lessons for tidy data.
                UPDATE class_sessions
                    SET ""PublishedAt"" = COALESCE(""PublishedAt"", ""CreatedAt"")
                    WHERE ""IsPublished"" = TRUE AND ""PublishedAt"" IS NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE class_sessions DROP COLUMN IF EXISTS ""VisibleUntil"";
                ALTER TABLE class_sessions DROP COLUMN IF EXISTS ""VisibleFrom"";
                ALTER TABLE class_sessions DROP COLUMN IF EXISTS ""PublishedAt"";
                ALTER TABLE class_sessions DROP COLUMN IF EXISTS ""IsPublished"";
            ");
        }
    }
}
