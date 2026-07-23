using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// Reusable, teacher-authored exercise sets attached to classes:
    ///   • exercise_sets — a named collection owned by a teacher/admin.
    ///   • exercise_set_classes — attaches a set to classes (its presence is the grant).
    ///   • lesson_exercises gains a SECOND, mutually-exclusive owner: "ExerciseSetId"
    ///     (nullable), and "LessonId" is relaxed to nullable. A CHECK enforces exactly one
    ///     owner. A new unique index (ExerciseSetId, OrderIndex) mirrors the lesson one so
    ///     the bulk-upsert is keyed the same way. lesson_exercise_submissions is UNTOUCHED —
    ///     it keys on (LessonExerciseId, UserId), so the submit / self-check / XP / grading
    ///     engine works for set exercises with zero change.
    ///
    /// House style: hand-authored + idempotent (IF NOT EXISTS / guarded constraint adds).
    /// Safe to re-run. Snapshot intentionally not machine-regenerated (migrations are
    /// hand-authored raw SQL, applied via the migration list, not model-diff scaffolding).
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260724000000_ExerciseSets")]
    public partial class ExerciseSets : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS exercise_sets (
                    ""Id"" uuid NOT NULL,
                    ""Title"" character varying(200) NOT NULL,
                    ""Description"" character varying(2000) NULL,
                    ""CreatedByUserId"" uuid NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_exercise_sets PRIMARY KEY (""Id""),
                    CONSTRAINT fk_exercise_sets_users FOREIGN KEY (""CreatedByUserId"")
                        REFERENCES users (""Id"") ON DELETE RESTRICT
                );
                CREATE INDEX IF NOT EXISTS ix_exercise_sets_created_by
                    ON exercise_sets (""CreatedByUserId"");

                CREATE TABLE IF NOT EXISTS exercise_set_classes (
                    ""Id"" uuid NOT NULL,
                    ""ExerciseSetId"" uuid NOT NULL,
                    ""ClassId"" uuid NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_exercise_set_classes PRIMARY KEY (""Id""),
                    CONSTRAINT fk_esc_sets FOREIGN KEY (""ExerciseSetId"")
                        REFERENCES exercise_sets (""Id"") ON DELETE CASCADE,
                    CONSTRAINT fk_esc_classes FOREIGN KEY (""ClassId"")
                        REFERENCES classes (""Id"") ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ix_exercise_set_classes_set_class
                    ON exercise_set_classes (""ExerciseSetId"", ""ClassId"");
                CREATE INDEX IF NOT EXISTS ix_exercise_set_classes_class
                    ON exercise_set_classes (""ClassId"");

                -- lesson_exercises: add the set owner alongside the lesson owner.
                ALTER TABLE lesson_exercises ALTER COLUMN ""LessonId"" DROP NOT NULL;
                ALTER TABLE lesson_exercises ADD COLUMN IF NOT EXISTS ""ExerciseSetId"" uuid NULL;

                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_lesson_exercises_exercise_sets') THEN
                        ALTER TABLE lesson_exercises ADD CONSTRAINT fk_lesson_exercises_exercise_sets
                            FOREIGN KEY (""ExerciseSetId"") REFERENCES exercise_sets (""Id"") ON DELETE CASCADE;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'ck_lesson_exercises_owner') THEN
                        ALTER TABLE lesson_exercises ADD CONSTRAINT ck_lesson_exercises_owner
                            CHECK ((""LessonId"" IS NULL) <> (""ExerciseSetId"" IS NULL));
                    END IF;
                END $$;

                CREATE UNIQUE INDEX IF NOT EXISTS ix_lesson_exercises_set_order
                    ON lesson_exercises (""ExerciseSetId"", ""OrderIndex"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ix_lesson_exercises_set_order;
                ALTER TABLE lesson_exercises DROP CONSTRAINT IF EXISTS ck_lesson_exercises_owner;
                -- Remove any set-owned exercises before restoring the NOT NULL on LessonId.
                DELETE FROM lesson_exercises WHERE ""ExerciseSetId"" IS NOT NULL;
                ALTER TABLE lesson_exercises DROP CONSTRAINT IF EXISTS fk_lesson_exercises_exercise_sets;
                ALTER TABLE lesson_exercises DROP COLUMN IF EXISTS ""ExerciseSetId"";
                ALTER TABLE lesson_exercises ALTER COLUMN ""LessonId"" SET NOT NULL;
                DROP TABLE IF EXISTS exercise_set_classes;
                DROP TABLE IF EXISTS exercise_sets;
            ");
        }
    }
}
