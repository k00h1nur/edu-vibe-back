using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// mock_test_slots — bookable mock-test sessions for the marketing site's
    /// Mock Test page. Admin-managed (Marketing.Manage), public read of active +
    /// upcoming slots. Raw idempotent SQL in house style; [Migration] on the
    /// class, snapshot patched by hand.
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260619000000_MockTestSlots")]
    public partial class MockTestSlots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS mock_test_slots (
                    ""Id"" uuid NOT NULL,
                    ""Title"" character varying(256) NOT NULL,
                    ""StartsAt"" timestamp with time zone NOT NULL,
                    ""DurationText"" character varying(64) NULL,
                    ""Capacity"" integer NOT NULL DEFAULT 0,
                    ""AvailableSeats"" integer NOT NULL DEFAULT 0,
                    ""SortOrder"" integer NOT NULL DEFAULT 0,
                    ""IsActive"" boolean NOT NULL DEFAULT true,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_mock_test_slots PRIMARY KEY (""Id"")
                );
                CREATE INDEX IF NOT EXISTS ix_mock_test_slots_is_active_starts_at
                    ON mock_test_slots (""IsActive"", ""StartsAt"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS mock_test_slots;");
        }
    }
}
