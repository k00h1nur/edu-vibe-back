using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// Lesson content layer — turns each ClassSession into a learning hub:
    ///   • class_sessions."VideoUrl"     — optional embedded video lesson.
    ///   • assignments."ClassSessionId"  — optional link of an assignment to a lesson.
    ///   • lesson_materials              — files attached to a lesson (FK cascade).
    ///   • lesson_progress               — per-(student, lesson) completion mark.
    /// Raw idempotent SQL in house style; [Migration] on the class, snapshot
    /// patched by hand.
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260617000000_LessonContentLayer")]
    public partial class LessonContentLayer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='class_sessions' AND column_name='VideoUrl') THEN
                        ALTER TABLE class_sessions ADD COLUMN ""VideoUrl"" text NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='assignments' AND column_name='ClassSessionId') THEN
                        ALTER TABLE assignments ADD COLUMN ""ClassSessionId"" uuid NULL;
                    END IF;
                END $$;
                CREATE INDEX IF NOT EXISTS ix_assignments_class_session_id ON assignments (""ClassSessionId"");
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS lesson_materials (
                    ""Id"" uuid NOT NULL,
                    ""ClassSessionId"" uuid NOT NULL,
                    ""StoredFileName"" character varying(256) NOT NULL,
                    ""OriginalFileName"" character varying(512) NOT NULL,
                    ""MimeType"" character varying(256) NOT NULL,
                    ""FileSize"" bigint NOT NULL,
                    ""UploadedByUserId"" uuid NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_lesson_materials PRIMARY KEY (""Id""),
                    CONSTRAINT fk_lesson_materials_class_sessions FOREIGN KEY (""ClassSessionId"")
                        REFERENCES class_sessions (""Id"") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ix_lesson_materials_class_session_id ON lesson_materials (""ClassSessionId"");
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS lesson_progress (
                    ""Id"" uuid NOT NULL,
                    ""StudentProfileId"" uuid NOT NULL,
                    ""ClassSessionId"" uuid NOT NULL,
                    ""CompletedAt"" timestamp with time zone NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_lesson_progress PRIMARY KEY (""Id"")
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ix_lesson_progress_student_session
                    ON lesson_progress (""StudentProfileId"", ""ClassSessionId"");
                CREATE INDEX IF NOT EXISTS ix_lesson_progress_class_session_id ON lesson_progress (""ClassSessionId"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS lesson_progress;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS lesson_materials;");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ix_assignments_class_session_id;");
            migrationBuilder.Sql(@"ALTER TABLE assignments DROP COLUMN IF EXISTS ""ClassSessionId"";");
            migrationBuilder.Sql(@"ALTER TABLE class_sessions DROP COLUMN IF EXISTS ""VideoUrl"";");
        }
    }
}
