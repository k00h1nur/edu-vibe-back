using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// Curriculum template engine (Phase 1):
    ///   • curriculum_templates / _modules / _units / _lessons — the reusable
    ///     learning-path tree.
    ///   • classes."CurriculumTemplateId"        — the template a class follows.
    ///   • class_sessions."CurriculumLessonId"   — the lesson a session teaches
    ///     (the schedule↔curriculum link powering "today's topic").
    /// Fully ADDITIVE + idempotent: the new columns are nullable with ON DELETE
    /// SET NULL, so every existing class/session keeps working unchanged
    /// (NULL = ad-hoc, pre-curriculum behaviour). Raw idempotent SQL in house
    /// style; snapshot patched by hand.
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260619000000_CurriculumEngine")]
    public partial class CurriculumEngine : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS curriculum_templates (
                    ""Id"" uuid NOT NULL,
                    ""Name"" character varying(200) NOT NULL,
                    ""Category"" integer NOT NULL,
                    ""Level"" character varying(40) NULL,
                    ""Description"" character varying(2000) NULL,
                    ""IsSystem"" boolean NOT NULL DEFAULT FALSE,
                    ""IsPublished"" boolean NOT NULL DEFAULT TRUE,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_curriculum_templates PRIMARY KEY (""Id"")
                );
                CREATE INDEX IF NOT EXISTS ix_curriculum_templates_category ON curriculum_templates (""Category"");

                CREATE TABLE IF NOT EXISTS curriculum_modules (
                    ""Id"" uuid NOT NULL,
                    ""TemplateId"" uuid NOT NULL,
                    ""Order"" integer NOT NULL,
                    ""Title"" character varying(200) NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_curriculum_modules PRIMARY KEY (""Id""),
                    CONSTRAINT fk_curriculum_modules_templates FOREIGN KEY (""TemplateId"")
                        REFERENCES curriculum_templates (""Id"") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ix_curriculum_modules_template_order ON curriculum_modules (""TemplateId"", ""Order"");

                CREATE TABLE IF NOT EXISTS curriculum_units (
                    ""Id"" uuid NOT NULL,
                    ""ModuleId"" uuid NOT NULL,
                    ""Order"" integer NOT NULL,
                    ""Title"" character varying(200) NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_curriculum_units PRIMARY KEY (""Id""),
                    CONSTRAINT fk_curriculum_units_modules FOREIGN KEY (""ModuleId"")
                        REFERENCES curriculum_modules (""Id"") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ix_curriculum_units_module_order ON curriculum_units (""ModuleId"", ""Order"");

                CREATE TABLE IF NOT EXISTS curriculum_lessons (
                    ""Id"" uuid NOT NULL,
                    ""UnitId"" uuid NOT NULL,
                    ""Order"" integer NOT NULL,
                    ""Title"" character varying(300) NOT NULL,
                    ""Objectives"" character varying(2000) NULL,
                    ""HomeworkPlaceholder"" character varying(1000) NULL,
                    ""MaterialsPlaceholder"" character varying(1000) NULL,
                    ""IsAssessment"" boolean NOT NULL DEFAULT FALSE,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_curriculum_lessons PRIMARY KEY (""Id""),
                    CONSTRAINT fk_curriculum_lessons_units FOREIGN KEY (""UnitId"")
                        REFERENCES curriculum_units (""Id"") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ix_curriculum_lessons_unit_order ON curriculum_lessons (""UnitId"", ""Order"");
            ");

            // Additive links on the existing tables — nullable, SET NULL on delete.
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='classes' AND column_name='CurriculumTemplateId') THEN
                        ALTER TABLE classes ADD COLUMN ""CurriculumTemplateId"" uuid NULL;
                        ALTER TABLE classes ADD CONSTRAINT fk_classes_curriculum_templates
                            FOREIGN KEY (""CurriculumTemplateId"") REFERENCES curriculum_templates (""Id"") ON DELETE SET NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='class_sessions' AND column_name='CurriculumLessonId') THEN
                        ALTER TABLE class_sessions ADD COLUMN ""CurriculumLessonId"" uuid NULL;
                        ALTER TABLE class_sessions ADD CONSTRAINT fk_class_sessions_curriculum_lessons
                            FOREIGN KEY (""CurriculumLessonId"") REFERENCES curriculum_lessons (""Id"") ON DELETE SET NULL;
                    END IF;
                END $$;
                CREATE INDEX IF NOT EXISTS ix_classes_curriculum_template_id ON classes (""CurriculumTemplateId"");
                CREATE INDEX IF NOT EXISTS ix_class_sessions_curriculum_lesson_id ON class_sessions (""CurriculumLessonId"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE class_sessions DROP CONSTRAINT IF EXISTS fk_class_sessions_curriculum_lessons;
                ALTER TABLE class_sessions DROP COLUMN IF EXISTS ""CurriculumLessonId"";
                ALTER TABLE classes DROP CONSTRAINT IF EXISTS fk_classes_curriculum_templates;
                ALTER TABLE classes DROP COLUMN IF EXISTS ""CurriculumTemplateId"";
                DROP TABLE IF EXISTS curriculum_lessons;
                DROP TABLE IF EXISTS curriculum_units;
                DROP TABLE IF EXISTS curriculum_modules;
                DROP TABLE IF EXISTS curriculum_templates;
            ");
        }
    }
}
