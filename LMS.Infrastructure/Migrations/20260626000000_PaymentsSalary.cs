using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// F5 — group pricing, monthly student payments and teacher salary config.
    /// Additive + idempotent:
    ///  • classes."MonthlyPrice" (nullable numeric)
    ///  • payments."ClassId" (nullable, FK→classes, SET NULL) + "PeriodMonth"
    ///    (date, backfilled from CreatedAt month then NOT NULL + default)
    ///  • teacher_salary_configs (one default + one per-class row per teacher,
    ///    unique on (TeacherId, ClassId) NULLS NOT DISTINCT)
    /// PaymentStatus gains Overdue=4 — enum is stored as int, no schema change.
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260626000000_PaymentsSalary")]
    public partial class PaymentsSalary : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE classes ADD COLUMN IF NOT EXISTS ""MonthlyPrice"" numeric(18,2) NULL;

                ALTER TABLE payments ADD COLUMN IF NOT EXISTS ""ClassId"" uuid NULL;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='payments' AND column_name='PeriodMonth') THEN
                        ALTER TABLE payments ADD COLUMN ""PeriodMonth"" date NULL;
                        UPDATE payments SET ""PeriodMonth"" = date_trunc('month', ""CreatedAt"")::date
                            WHERE ""PeriodMonth"" IS NULL;
                        ALTER TABLE payments ALTER COLUMN ""PeriodMonth"" SET NOT NULL;
                        ALTER TABLE payments ALTER COLUMN ""PeriodMonth""
                            SET DEFAULT (date_trunc('month', (now() at time zone 'utc'))::date);
                    END IF;
                END $$;

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname='fk_payments_class') THEN
                        ALTER TABLE payments ADD CONSTRAINT fk_payments_class
                            FOREIGN KEY (""ClassId"") REFERENCES classes (""Id"") ON DELETE SET NULL;
                    END IF;
                END $$;

                CREATE INDEX IF NOT EXISTS ix_payments_class_period ON payments (""ClassId"", ""PeriodMonth"");

                CREATE TABLE IF NOT EXISTS teacher_salary_configs (
                    ""Id"" uuid NOT NULL,
                    ""TeacherId"" uuid NOT NULL,
                    ""ClassId"" uuid NULL,
                    ""Percentage"" numeric(5,2) NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_teacher_salary_configs PRIMARY KEY (""Id""),
                    CONSTRAINT fk_tsc_teacher FOREIGN KEY (""TeacherId"") REFERENCES users (""Id"") ON DELETE RESTRICT,
                    CONSTRAINT fk_tsc_class FOREIGN KEY (""ClassId"") REFERENCES classes (""Id"") ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ux_tsc_teacher_class
                    ON teacher_salary_configs (""TeacherId"", ""ClassId"") NULLS NOT DISTINCT;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS teacher_salary_configs;
                DROP INDEX IF EXISTS ix_payments_class_period;
                ALTER TABLE payments DROP CONSTRAINT IF EXISTS fk_payments_class;
                ALTER TABLE payments DROP COLUMN IF EXISTS ""PeriodMonth"";
                ALTER TABLE payments DROP COLUMN IF EXISTS ""ClassId"";
                ALTER TABLE classes DROP COLUMN IF EXISTS ""MonthlyPrice"";
            ");
        }
    }
}
