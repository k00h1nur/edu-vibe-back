using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// F8 — offline exams. Additive + idempotent. An exam is configured on an
    /// exam-type curriculum lesson (1:1) and owned by a class; sections carry a max
    /// score; results are entered manually per student with a per-section breakdown.
    /// Timestamp 20260701 sits after the unmerged F6 (20260629) to avoid collision.
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260701000000_OfflineExams")]
    public partial class OfflineExams : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS exams (
                    ""Id"" uuid NOT NULL,
                    ""ClassId"" uuid NOT NULL,
                    ""CurriculumLessonId"" uuid NOT NULL,
                    ""Title"" character varying(256) NOT NULL,
                    ""PassThresholdPercent"" numeric(5,2) NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_exams PRIMARY KEY (""Id""),
                    CONSTRAINT fk_exams_classes FOREIGN KEY (""ClassId"") REFERENCES classes (""Id"") ON DELETE CASCADE,
                    CONSTRAINT fk_exams_curriculum_lessons FOREIGN KEY (""CurriculumLessonId"")
                        REFERENCES curriculum_lessons (""Id"") ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ux_exams_curriculum_lesson ON exams (""CurriculumLessonId"");

                CREATE TABLE IF NOT EXISTS exam_sections (
                    ""Id"" uuid NOT NULL,
                    ""ExamId"" uuid NOT NULL,
                    ""Name"" character varying(128) NOT NULL,
                    ""MaxScore"" numeric(9,2) NOT NULL,
                    ""Order"" integer NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_exam_sections PRIMARY KEY (""Id""),
                    CONSTRAINT fk_exam_sections_exams FOREIGN KEY (""ExamId"") REFERENCES exams (""Id"") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ix_exam_sections_exam_order ON exam_sections (""ExamId"", ""Order"");

                CREATE TABLE IF NOT EXISTS exam_results (
                    ""Id"" uuid NOT NULL,
                    ""ExamId"" uuid NOT NULL,
                    ""StudentProfileId"" uuid NOT NULL,
                    ""OverallPercent"" numeric(5,2) NOT NULL,
                    ""Passed"" boolean NOT NULL,
                    ""EnteredByUserId"" uuid NOT NULL,
                    ""EnteredAt"" timestamp with time zone NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_exam_results PRIMARY KEY (""Id""),
                    CONSTRAINT fk_exam_results_exams FOREIGN KEY (""ExamId"") REFERENCES exams (""Id"") ON DELETE CASCADE,
                    CONSTRAINT fk_exam_results_student_profiles FOREIGN KEY (""StudentProfileId"")
                        REFERENCES student_profiles (""Id"") ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ux_exam_results_exam_student ON exam_results (""ExamId"", ""StudentProfileId"");

                CREATE TABLE IF NOT EXISTS exam_section_scores (
                    ""Id"" uuid NOT NULL,
                    ""ExamResultId"" uuid NOT NULL,
                    ""ExamSectionId"" uuid NOT NULL,
                    ""Score"" numeric(9,2) NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_exam_section_scores PRIMARY KEY (""Id""),
                    CONSTRAINT fk_exam_section_scores_exam_results FOREIGN KEY (""ExamResultId"")
                        REFERENCES exam_results (""Id"") ON DELETE CASCADE,
                    CONSTRAINT fk_exam_section_scores_exam_sections FOREIGN KEY (""ExamSectionId"")
                        REFERENCES exam_sections (""Id"") ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ux_exam_section_scores_result_section
                    ON exam_section_scores (""ExamResultId"", ""ExamSectionId"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS exam_section_scores;
                DROP TABLE IF EXISTS exam_results;
                DROP TABLE IF EXISTS exam_sections;
                DROP TABLE IF EXISTS exams;
            ");
        }
    }
}
