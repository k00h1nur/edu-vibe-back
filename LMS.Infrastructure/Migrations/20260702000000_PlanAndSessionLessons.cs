using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// Template-level teaching plan + multi-lesson session model (rails only — no
    /// behaviour change; handlers wire these up in later PRs):
    ///   • curriculum_plan_days / curriculum_plan_day_lessons — a book's reusable
    ///     default plan: ordered "class days", each covering 1+ curriculum lessons
    ///     (1A+1B) or a single exam lesson. Hangs off curriculum_templates.
    ///   • class_session_lessons — a session 1:many curriculum lessons (the join
    ///     that lets one class day teach two coursebook lessons). The existing
    ///     class_sessions."CurriculumLessonId" stays as the denormalised "primary".
    ///   • assignments."CurriculumLessonId" — provenance so a day's per-lesson
    ///     homework stays separately reconcilable (one Assignment per session+lesson).
    ///
    /// Fully ADDITIVE + idempotent (house style): CREATE TABLE IF NOT EXISTS,
    /// DO $$ IF NOT EXISTS for the column, and re-runnable backfills (insert the
    /// join from the existing single FK; fill assignment provenance from the
    /// session's lesson). Re-running on a populated DB is a safe no-op. Snapshot
    /// patched by hand (these tables aren't EF entities yet — added when consumed).
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260702000000_PlanAndSessionLessons")]
    public partial class PlanAndSessionLessons : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---- New tables -------------------------------------------------
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS curriculum_plan_days (
                    ""Id"" uuid NOT NULL,
                    ""TemplateId"" uuid NOT NULL,
                    ""Order"" integer NOT NULL,
                    ""Title"" character varying(200) NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_curriculum_plan_days PRIMARY KEY (""Id""),
                    CONSTRAINT fk_curriculum_plan_days_templates FOREIGN KEY (""TemplateId"")
                        REFERENCES curriculum_templates (""Id"") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ix_curriculum_plan_days_template_order
                    ON curriculum_plan_days (""TemplateId"", ""Order"");

                CREATE TABLE IF NOT EXISTS curriculum_plan_day_lessons (
                    ""Id"" uuid NOT NULL,
                    ""PlanDayId"" uuid NOT NULL,
                    ""CurriculumLessonId"" uuid NOT NULL,
                    ""Order"" integer NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_curriculum_plan_day_lessons PRIMARY KEY (""Id""),
                    CONSTRAINT fk_cpdl_plan_days FOREIGN KEY (""PlanDayId"")
                        REFERENCES curriculum_plan_days (""Id"") ON DELETE CASCADE,
                    CONSTRAINT fk_cpdl_lessons FOREIGN KEY (""CurriculumLessonId"")
                        REFERENCES curriculum_lessons (""Id"") ON DELETE CASCADE,
                    CONSTRAINT uq_cpdl_day_lesson UNIQUE (""PlanDayId"", ""CurriculumLessonId"")
                );
                CREATE INDEX IF NOT EXISTS ix_cpdl_day_order
                    ON curriculum_plan_day_lessons (""PlanDayId"", ""Order"");

                CREATE TABLE IF NOT EXISTS class_session_lessons (
                    ""Id"" uuid NOT NULL,
                    ""ClassSessionId"" uuid NOT NULL,
                    ""CurriculumLessonId"" uuid NOT NULL,
                    ""Order"" integer NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_class_session_lessons PRIMARY KEY (""Id""),
                    CONSTRAINT fk_csl_sessions FOREIGN KEY (""ClassSessionId"")
                        REFERENCES class_sessions (""Id"") ON DELETE CASCADE,
                    CONSTRAINT fk_csl_lessons FOREIGN KEY (""CurriculumLessonId"")
                        REFERENCES curriculum_lessons (""Id"") ON DELETE CASCADE,
                    CONSTRAINT uq_csl_session_lesson UNIQUE (""ClassSessionId"", ""CurriculumLessonId"")
                );
                CREATE INDEX IF NOT EXISTS ix_csl_session_order
                    ON class_session_lessons (""ClassSessionId"", ""Order"");
            ");

            // ---- Assignment provenance column (additive, SET NULL) ----------
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='assignments' AND column_name='CurriculumLessonId') THEN
                        ALTER TABLE assignments ADD COLUMN ""CurriculumLessonId"" uuid NULL;
                        ALTER TABLE assignments ADD CONSTRAINT fk_assignments_curriculum_lessons
                            FOREIGN KEY (""CurriculumLessonId"") REFERENCES curriculum_lessons (""Id"") ON DELETE SET NULL;
                    END IF;
                END $$;
                CREATE INDEX IF NOT EXISTS ix_assignments_curriculum_lesson_id
                    ON assignments (""CurriculumLessonId"");
            ");

            // ---- Idempotent, re-runnable backfills --------------------------
            // (1) Seed the join from the existing single FK — one row per session
            //     that already links a lesson. Guarded by the unique key, so a
            //     re-run inserts nothing.
            migrationBuilder.Sql(@"
                INSERT INTO class_session_lessons (""Id"", ""ClassSessionId"", ""CurriculumLessonId"", ""Order"")
                SELECT gen_random_uuid(), s.""Id"", s.""CurriculumLessonId"", 1
                FROM class_sessions s
                WHERE s.""CurriculumLessonId"" IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM class_session_lessons x
                      WHERE x.""ClassSessionId"" = s.""Id"" AND x.""CurriculumLessonId"" = s.""CurriculumLessonId"");
            ");

            // (2) Fill assignment provenance from the session's lesson. The #51
            //     up-front materialiser created one assignment per session from one
            //     lesson, so this is exact. Only touches NULLs → re-run is a no-op.
            migrationBuilder.Sql(@"
                UPDATE assignments a
                SET ""CurriculumLessonId"" = s.""CurriculumLessonId""
                FROM class_sessions s
                WHERE a.""ClassSessionId"" = s.""Id""
                  AND a.""CurriculumLessonId"" IS NULL
                  AND s.""CurriculumLessonId"" IS NOT NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE assignments DROP CONSTRAINT IF EXISTS fk_assignments_curriculum_lessons;
                ALTER TABLE assignments DROP COLUMN IF EXISTS ""CurriculumLessonId"";
                DROP TABLE IF EXISTS class_session_lessons;
                DROP TABLE IF EXISTS curriculum_plan_day_lessons;
                DROP TABLE IF EXISTS curriculum_plan_days;
            ");
        }
    }
}
