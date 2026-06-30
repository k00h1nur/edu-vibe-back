using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// Lesson self-check exercises (textbook-style practice), separate from the graded
    /// assignment LearningTask/TaskSubmission pipeline:
    ///   • lesson_exercises — type-tagged (free string) practice items on a curriculum
    ///     lesson; "ContentJson" is jsonb whose shape varies by type. Unique per
    ///     (LessonId, OrderIndex) so the bulk "add exercises" upsert is keyed.
    ///   • lesson_exercise_submissions — one self-check result per (exercise, user);
    ///     "AnswersJson" jsonb + Score/Total. Unique per (LessonExerciseId, UserId).
    ///
    /// House style: hand-authored + idempotent (CREATE TABLE IF NOT EXISTS). Re-running
    /// on a populated DB is a safe no-op. Snapshot patched by hand.
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260703000000_LessonExercises")]
    public partial class LessonExercises : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS lesson_exercises (
                    ""Id"" uuid NOT NULL,
                    ""LessonId"" uuid NOT NULL,
                    ""Type"" character varying(40) NOT NULL,
                    ""Title"" character varying(300) NULL,
                    ""OrderIndex"" integer NOT NULL,
                    ""ContentJson"" jsonb NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_lesson_exercises PRIMARY KEY (""Id""),
                    CONSTRAINT fk_lesson_exercises_curriculum_lessons FOREIGN KEY (""LessonId"")
                        REFERENCES curriculum_lessons (""Id"") ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ix_lesson_exercises_lesson_order
                    ON lesson_exercises (""LessonId"", ""OrderIndex"");

                CREATE TABLE IF NOT EXISTS lesson_exercise_submissions (
                    ""Id"" uuid NOT NULL,
                    ""LessonExerciseId"" uuid NOT NULL,
                    ""UserId"" uuid NOT NULL,
                    ""AnswersJson"" jsonb NOT NULL,
                    ""Score"" integer NOT NULL,
                    ""Total"" integer NOT NULL,
                    ""IsCompleted"" boolean NOT NULL DEFAULT false,
                    ""CompletedAt"" timestamp with time zone NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_lesson_exercise_submissions PRIMARY KEY (""Id""),
                    CONSTRAINT fk_les_exercises FOREIGN KEY (""LessonExerciseId"")
                        REFERENCES lesson_exercises (""Id"") ON DELETE CASCADE,
                    CONSTRAINT fk_les_users FOREIGN KEY (""UserId"")
                        REFERENCES users (""Id"") ON DELETE CASCADE,
                    CONSTRAINT uq_les_exercise_user UNIQUE (""LessonExerciseId"", ""UserId"")
                );
                CREATE INDEX IF NOT EXISTS ix_lesson_exercise_submissions_user
                    ON lesson_exercise_submissions (""UserId"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS lesson_exercise_submissions;
                DROP TABLE IF EXISTS lesson_exercises;
            ");
        }
    }
}
