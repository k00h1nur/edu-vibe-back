using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// F1 — teacher punishments (salary deductions). Additive + idempotent:
    /// CREATE TABLE / INDEX IF NOT EXISTS. PeriodMonth is a date normalised to
    /// the 1st (platform-wide PeriodMonth convention).
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260625000000_Punishments")]
    public partial class Punishments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS punishments (
                    ""Id"" uuid NOT NULL,
                    ""TeacherId"" uuid NOT NULL,
                    ""Title"" character varying(256) NOT NULL,
                    ""Description"" character varying(2000) NULL,
                    ""Type"" integer NOT NULL,
                    ""Value"" numeric(18,2) NOT NULL,
                    ""Reason"" character varying(2000) NULL,
                    ""AppliedByAdminId"" uuid NOT NULL,
                    ""PeriodMonth"" date NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_punishments PRIMARY KEY (""Id""),
                    CONSTRAINT fk_punishments_teacher FOREIGN KEY (""TeacherId"")
                        REFERENCES users (""Id"") ON DELETE RESTRICT
                );
                CREATE INDEX IF NOT EXISTS ix_punishments_teacher_period
                    ON punishments (""TeacherId"", ""PeriodMonth"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS punishments;");
        }
    }
}
