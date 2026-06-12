using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// Recurring class schedules: one pattern row per class (weekday set,
    /// date range, time range). Concrete class_sessions rows are generated
    /// from the pattern by ApplyClassScheduleCommand, so teacher/student
    /// schedules and attendance keep consuming sessions unchanged.
    ///
    /// Raw idempotent SQL like the rest of the project — safe on fresh,
    /// partially-applied, and hand-patched DBs. The [Migration] attribute
    /// lives here directly (no Designer file): EF only needs the id +
    /// DbContext to apply it at runtime.
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260614000000_ClassSchedulePatterns")]
    public partial class ClassSchedulePatterns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS class_schedule_patterns (
                    ""Id"" uuid NOT NULL,
                    ""ClassId"" uuid NOT NULL,
                    ""Type"" integer NOT NULL,
                    ""DaysOfWeekMask"" integer NOT NULL,
                    ""StartDate"" date NOT NULL,
                    ""EndDate"" date NOT NULL,
                    ""StartsAt"" time without time zone NOT NULL,
                    ""EndsAt"" time without time zone NOT NULL,
                    ""RoomId"" uuid NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_class_schedule_patterns PRIMARY KEY (""Id""),
                    CONSTRAINT fk_class_schedule_patterns_classes FOREIGN KEY (""ClassId"")
                        REFERENCES classes (""Id"") ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ix_class_schedule_patterns_class_id
                    ON class_schedule_patterns (""ClassId"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS class_schedule_patterns;");
        }
    }
}
