using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// Three-in-one corrective migration:
    ///   • specializations + staff_specializations — the original
    ///     <c>20260609100236_Specializations</c> migration was scaffolded against
    ///     a stale build and only emitted seed-data UpdateData churn, no
    ///     CreateTable calls. The runtime explodes with
    ///     "42P01 relation specializations does not exist" the first time the
    ///     admin Specializations page loads.
    ///   • office_info — new singleton row holding the academy's public contact
    ///     + branding info. Drives both the admin Office Info screen and the
    ///     marketing site's Contact section.
    ///   • announcements — new academy-wide announcement table, surfaces on
    ///     the student dashboard widget and the marketing news strip.
    ///
    /// All four CREATE TABLEs use IF NOT EXISTS so it's safe on a fresh DB, a
    /// partially-applied DB (eg someone hand-created the specializations
    /// table), or re-runs.
    /// </summary>
    public partial class SpecializationsOfficeInfoAnnouncementsFix : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS specializations (
                    ""Id"" uuid NOT NULL,
                    ""Code"" character varying(64) NOT NULL,
                    ""Name"" character varying(128) NOT NULL,
                    ""IsActive"" boolean NOT NULL DEFAULT TRUE,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_specializations PRIMARY KEY (""Id"")
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ix_specializations_code ON specializations (""Code"");
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS staff_specializations (
                    ""Id"" uuid NOT NULL,
                    ""StaffProfileId"" uuid NOT NULL,
                    ""SpecializationId"" uuid NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_staff_specializations PRIMARY KEY (""Id""),
                    CONSTRAINT fk_staff_specializations_staff FOREIGN KEY (""StaffProfileId"")
                        REFERENCES staff_profiles (""Id"") ON DELETE CASCADE,
                    CONSTRAINT fk_staff_specializations_specializations FOREIGN KEY (""SpecializationId"")
                        REFERENCES specializations (""Id"") ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ix_staff_specializations_pair
                    ON staff_specializations (""StaffProfileId"", ""SpecializationId"");
                CREATE INDEX IF NOT EXISTS ix_staff_specializations_specialization
                    ON staff_specializations (""SpecializationId"");
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS office_info (
                    ""Id"" uuid NOT NULL,
                    ""AcademyName"" character varying(256) NOT NULL,
                    ""Tagline"" character varying(256) NULL,
                    ""PhoneNumber"" character varying(64) NULL,
                    ""SecondaryPhone"" character varying(64) NULL,
                    ""Email"" character varying(320) NULL,
                    ""Address"" character varying(512) NULL,
                    ""WorkingHours"" character varying(256) NULL,
                    ""TelegramUrl"" character varying(512) NULL,
                    ""InstagramUrl"" character varying(512) NULL,
                    ""FacebookUrl"" character varying(512) NULL,
                    ""YoutubeUrl"" character varying(512) NULL,
                    ""WebsiteUrl"" character varying(512) NULL,
                    ""AboutHtml"" character varying(8000) NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_office_info PRIMARY KEY (""Id"")
                );
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS announcements (
                    ""Id"" uuid NOT NULL,
                    ""Title"" character varying(256) NOT NULL,
                    ""Body"" character varying(4000) NOT NULL,
                    ""IsPublic"" boolean NOT NULL DEFAULT FALSE,
                    ""PublishesAt"" timestamp with time zone NULL,
                    ""ExpiresAt"" timestamp with time zone NULL,
                    ""AuthorUserId"" uuid NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_announcements PRIMARY KEY (""Id""),
                    CONSTRAINT fk_announcements_users FOREIGN KEY (""AuthorUserId"")
                        REFERENCES users (""Id"") ON DELETE RESTRICT
                );
                CREATE INDEX IF NOT EXISTS ix_announcements_visibility_window
                    ON announcements (""IsPublic"", ""PublishesAt"", ""ExpiresAt"");
                CREATE INDEX IF NOT EXISTS ix_announcements_created_at ON announcements (""CreatedAt"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS announcements;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS office_info;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS staff_specializations;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS specializations;");
        }
    }
}
