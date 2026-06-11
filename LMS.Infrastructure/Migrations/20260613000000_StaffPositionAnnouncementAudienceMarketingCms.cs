using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// Multi-feature corrective migration:
    ///   • staff_profiles gets Position + IsPubliclyVisible columns (drives
    ///     the marketing-site teachers grid and admin edit form);
    ///   • announcements gets an Audience column (Everyone/Teachers/Students)
    ///     so admin can target;
    ///   • marketing_courses + marketing_videos are brand-new tables for
    ///     the marketing CMS.
    ///
    /// Raw idempotent SQL like the rest of the project — safe on fresh,
    /// partially-applied, and hand-patched DBs.
    /// </summary>
    public partial class StaffPositionAnnouncementAudienceMarketingCms : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // staff_profiles.Position + IsPubliclyVisible
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'staff_profiles' AND column_name = 'Position'
                    ) THEN
                        ALTER TABLE staff_profiles ADD COLUMN ""Position"" character varying(128) NULL;
                    END IF;
                END $$;
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'staff_profiles' AND column_name = 'IsPubliclyVisible'
                    ) THEN
                        ALTER TABLE staff_profiles ADD COLUMN ""IsPubliclyVisible"" boolean NOT NULL DEFAULT FALSE;
                    END IF;
                END $$;
            ");

            // announcements.Audience
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'announcements' AND column_name = 'Audience'
                    ) THEN
                        -- Defaults existing rows to Everyone (1) so historical
                        -- announcements stay visible to everyone signed in.
                        ALTER TABLE announcements ADD COLUMN ""Audience"" integer NOT NULL DEFAULT 1;
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS marketing_courses (
                    ""Id"" uuid NOT NULL,
                    ""Slug"" character varying(64) NOT NULL,
                    ""Title"" character varying(256) NOT NULL,
                    ""Subtitle"" character varying(256) NULL,
                    ""Description"" character varying(4000) NULL,
                    ""CoverImageUrl"" character varying(1024) NULL,
                    ""PriceText"" character varying(64) NULL,
                    ""DurationText"" character varying(64) NULL,
                    ""LevelText"" character varying(64) NULL,
                    ""SortOrder"" integer NOT NULL DEFAULT 0,
                    ""IsActive"" boolean NOT NULL DEFAULT TRUE,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_marketing_courses PRIMARY KEY (""Id"")
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ix_marketing_courses_slug ON marketing_courses (""Slug"");
                CREATE INDEX IF NOT EXISTS ix_marketing_courses_active_order
                    ON marketing_courses (""IsActive"", ""SortOrder"");
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS marketing_videos (
                    ""Id"" uuid NOT NULL,
                    ""Title"" character varying(256) NOT NULL,
                    ""Description"" character varying(2000) NULL,
                    ""VideoUrl"" character varying(1024) NOT NULL,
                    ""ThumbnailUrl"" character varying(1024) NULL,
                    ""SortOrder"" integer NOT NULL DEFAULT 0,
                    ""IsActive"" boolean NOT NULL DEFAULT TRUE,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_marketing_videos PRIMARY KEY (""Id"")
                );
                CREATE INDEX IF NOT EXISTS ix_marketing_videos_active_order
                    ON marketing_videos (""IsActive"", ""SortOrder"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS marketing_videos;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS marketing_courses;");
            migrationBuilder.Sql(@"ALTER TABLE announcements DROP COLUMN IF EXISTS ""Audience"";");
            migrationBuilder.Sql(@"ALTER TABLE staff_profiles DROP COLUMN IF EXISTS ""IsPubliclyVisible"";");
            migrationBuilder.Sql(@"ALTER TABLE staff_profiles DROP COLUMN IF EXISTS ""Position"";");
        }
    }
}
