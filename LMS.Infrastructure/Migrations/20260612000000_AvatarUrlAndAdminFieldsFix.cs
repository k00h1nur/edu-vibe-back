using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// Adds the columns the previous migration (AvatarsRemindersAndAdminStudentFields)
    /// failed to scaffold because the EF tool was invoked against a stale build —
    /// it picked up only the seed-data UpdateData churn, not the new domain
    /// properties. This migration restores parity: AvatarUrl on staff_profiles
    /// and student_profiles, ParentPhoneNumber on student_profiles. Reminders
    /// table and Level column are also re-checked with IF NOT EXISTS-style
    /// raw SQL so it's idempotent against partially-applied DBs.
    /// </summary>
    public partial class AvatarUrlAndAdminFieldsFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // staff_profiles.AvatarUrl
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'staff_profiles' AND column_name = 'AvatarUrl'
                    ) THEN
                        ALTER TABLE staff_profiles ADD COLUMN ""AvatarUrl"" character varying(1024) NULL;
                    END IF;
                END $$;
            ");

            // student_profiles.AvatarUrl
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'student_profiles' AND column_name = 'AvatarUrl'
                    ) THEN
                        ALTER TABLE student_profiles ADD COLUMN ""AvatarUrl"" character varying(1024) NULL;
                    END IF;
                END $$;
            ");

            // student_profiles.ParentPhoneNumber
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'student_profiles' AND column_name = 'ParentPhoneNumber'
                    ) THEN
                        ALTER TABLE student_profiles ADD COLUMN ""ParentPhoneNumber"" character varying(32) NULL;
                    END IF;
                END $$;
            ");

            // student_profiles.Level (already in the snapshot but defensively re-checked)
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'student_profiles' AND column_name = 'Level'
                    ) THEN
                        ALTER TABLE student_profiles ADD COLUMN ""Level"" character varying(32) NULL;
                    END IF;
                END $$;
            ");

            // Reminders table — only created if missing.
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS reminders (
                    ""Id"" uuid NOT NULL,
                    ""OwnerUserId"" uuid NOT NULL,
                    ""Title"" character varying(256) NOT NULL,
                    ""Notes"" character varying(2000) NULL,
                    ""DueAt"" timestamp with time zone NOT NULL,
                    ""IsCompleted"" boolean NOT NULL DEFAULT FALSE,
                    ""CompletedAt"" timestamp with time zone NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_reminders PRIMARY KEY (""Id""),
                    CONSTRAINT fk_reminders_users_owner FOREIGN KEY (""OwnerUserId"")
                        REFERENCES users (""Id"") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ix_reminders_owner_due ON reminders (""OwnerUserId"", ""DueAt"");
                CREATE INDEX IF NOT EXISTS ix_reminders_owner_status ON reminders (""OwnerUserId"", ""IsCompleted"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Best-effort rollback. Dropping columns loses data — if you ran
            // this migration in production you almost certainly want to keep
            // the columns even after a Down.
            migrationBuilder.Sql(@"ALTER TABLE staff_profiles DROP COLUMN IF EXISTS ""AvatarUrl"";");
            migrationBuilder.Sql(@"ALTER TABLE student_profiles DROP COLUMN IF EXISTS ""AvatarUrl"";");
            migrationBuilder.Sql(@"ALTER TABLE student_profiles DROP COLUMN IF EXISTS ""ParentPhoneNumber"";");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS reminders;");
        }
    }
}
