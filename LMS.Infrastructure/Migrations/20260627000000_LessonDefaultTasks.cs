using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// F3 — default/blueprint tasks attached to a curriculum lesson. Additive +
    /// idempotent: creates lesson_default_tasks (FK → curriculum_lessons, cascade)
    /// only if absent. Cloning a template onto a class deep-copies these rows.
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260627000000_LessonDefaultTasks")]
    public partial class LessonDefaultTasks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS lesson_default_tasks (
                    ""Id"" uuid NOT NULL,
                    ""CurriculumLessonId"" uuid NOT NULL,
                    ""Order"" integer NOT NULL,
                    ""Type"" integer NOT NULL,
                    ""Title"" character varying(256) NOT NULL,
                    ""Points"" integer NOT NULL,
                    ""ContentJson"" text NOT NULL,
                    ""SolutionJson"" text NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_lesson_default_tasks PRIMARY KEY (""Id""),
                    CONSTRAINT fk_lesson_default_tasks_curriculum_lessons FOREIGN KEY (""CurriculumLessonId"")
                        REFERENCES curriculum_lessons (""Id"") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ix_lesson_default_tasks_lesson_order
                    ON lesson_default_tasks (""CurriculumLessonId"", ""Order"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS lesson_default_tasks;");
        }
    }
}
