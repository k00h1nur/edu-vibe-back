using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// Turns a calendar slot into an online lesson: class_sessions gains an
    /// optional Topic (lesson title), MeetingUrl (Zoom / Google Meet link the
    /// student joins from) and Notes. All nullable — an in-person slot leaves
    /// them blank. Raw idempotent SQL in house style; [Migration] on the class,
    /// snapshot patched by hand.
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260616000000_ClassSessionLessonDetails")]
    public partial class ClassSessionLessonDetails : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='class_sessions' AND column_name='Topic') THEN
                        ALTER TABLE class_sessions ADD COLUMN ""Topic"" text NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='class_sessions' AND column_name='MeetingUrl') THEN
                        ALTER TABLE class_sessions ADD COLUMN ""MeetingUrl"" text NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='class_sessions' AND column_name='Notes') THEN
                        ALTER TABLE class_sessions ADD COLUMN ""Notes"" text NULL;
                    END IF;
                END $$;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE class_sessions DROP COLUMN IF EXISTS ""Notes"";");
            migrationBuilder.Sql(@"ALTER TABLE class_sessions DROP COLUMN IF EXISTS ""MeetingUrl"";");
            migrationBuilder.Sql(@"ALTER TABLE class_sessions DROP COLUMN IF EXISTS ""Topic"";");
        }
    }
}
