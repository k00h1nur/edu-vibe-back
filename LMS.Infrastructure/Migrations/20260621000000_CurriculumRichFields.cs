using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// Course Builder rich fields: unit icon / estimated minutes / XP reward, and
    /// lesson type / duration / XP reward. All additive + idempotent — nullable or
    /// defaulted columns, no existing data touched (LessonType + XpReward default
    /// to 0, which maps to <c>General</c> / no reward).
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260621000000_CurriculumRichFields")]
    public partial class CurriculumRichFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='curriculum_units' AND column_name='Icon') THEN
                        ALTER TABLE curriculum_units ADD COLUMN ""Icon"" character varying(16) NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='curriculum_units' AND column_name='EstimatedMinutes') THEN
                        ALTER TABLE curriculum_units ADD COLUMN ""EstimatedMinutes"" integer NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='curriculum_units' AND column_name='XpReward') THEN
                        ALTER TABLE curriculum_units ADD COLUMN ""XpReward"" integer NOT NULL DEFAULT 0;
                    END IF;

                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='curriculum_lessons' AND column_name='LessonType') THEN
                        ALTER TABLE curriculum_lessons ADD COLUMN ""LessonType"" integer NOT NULL DEFAULT 0;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='curriculum_lessons' AND column_name='DurationMinutes') THEN
                        ALTER TABLE curriculum_lessons ADD COLUMN ""DurationMinutes"" integer NULL;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='curriculum_lessons' AND column_name='XpReward') THEN
                        ALTER TABLE curriculum_lessons ADD COLUMN ""XpReward"" integer NOT NULL DEFAULT 0;
                    END IF;
                END $$;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE curriculum_units DROP COLUMN IF EXISTS ""Icon"";
                ALTER TABLE curriculum_units DROP COLUMN IF EXISTS ""EstimatedMinutes"";
                ALTER TABLE curriculum_units DROP COLUMN IF EXISTS ""XpReward"";
                ALTER TABLE curriculum_lessons DROP COLUMN IF EXISTS ""LessonType"";
                ALTER TABLE curriculum_lessons DROP COLUMN IF EXISTS ""DurationMinutes"";
                ALTER TABLE curriculum_lessons DROP COLUMN IF EXISTS ""XpReward"";
            ");
        }
    }
}
