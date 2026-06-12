using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// File-based assignment submissions + deadlines + anti-cheat scaffolding:
    ///   • assignments."DueDate"        — optional submission deadline (UTC).
    ///   • submissions."IsLocked"       — student can no longer add/remove files.
    ///   • submission_files             — many files per submission, each with
    ///     a SHA-256 for duplicate detection.
    ///   • submission_audits            — append-only upload/edit/lock log.
    ///
    /// Raw idempotent SQL in house style; the [Migration] attribute is on the
    /// class directly (no Designer needed at runtime) and the model snapshot
    /// is patched by hand.
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260615000000_SubmissionFilesAndDueDates")]
    public partial class SubmissionFilesAndDueDates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='assignments' AND column_name='DueDate') THEN
                        ALTER TABLE assignments ADD COLUMN ""DueDate"" timestamp with time zone NULL;
                    END IF;
                END $$;
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='submissions' AND column_name='IsLocked') THEN
                        ALTER TABLE submissions ADD COLUMN ""IsLocked"" boolean NOT NULL DEFAULT FALSE;
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS submission_files (
                    ""Id"" uuid NOT NULL,
                    ""SubmissionId"" uuid NOT NULL,
                    ""StoredFileName"" character varying(256) NOT NULL,
                    ""OriginalFileName"" character varying(512) NOT NULL,
                    ""MimeType"" character varying(256) NOT NULL,
                    ""FileSize"" bigint NOT NULL,
                    ""Sha256"" character varying(64) NOT NULL,
                    ""IsDuplicateAcrossStudents"" boolean NOT NULL DEFAULT FALSE,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_submission_files PRIMARY KEY (""Id""),
                    CONSTRAINT fk_submission_files_submissions FOREIGN KEY (""SubmissionId"")
                        REFERENCES submissions (""Id"") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ix_submission_files_submission_id ON submission_files (""SubmissionId"");
                CREATE INDEX IF NOT EXISTS ix_submission_files_sha256 ON submission_files (""Sha256"");
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS submission_audits (
                    ""Id"" uuid NOT NULL,
                    ""SubmissionId"" uuid NOT NULL,
                    ""ActorUserId"" uuid NULL,
                    ""Action"" character varying(32) NOT NULL,
                    ""Detail"" character varying(1024) NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_submission_audits PRIMARY KEY (""Id"")
                );
                CREATE INDEX IF NOT EXISTS ix_submission_audits_submission_id ON submission_audits (""SubmissionId"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS submission_audits;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS submission_files;");
            migrationBuilder.Sql(@"ALTER TABLE submissions DROP COLUMN IF EXISTS ""IsLocked"";");
            migrationBuilder.Sql(@"ALTER TABLE assignments DROP COLUMN IF EXISTS ""DueDate"";");
        }
    }
}
