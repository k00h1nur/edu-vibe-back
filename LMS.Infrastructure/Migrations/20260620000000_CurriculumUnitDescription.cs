using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// Course Builder: a short description on each curriculum unit (shown on the
    /// unit card). Additive + idempotent — nullable column, no data touched.
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260620000000_CurriculumUnitDescription")]
    public partial class CurriculumUnitDescription : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='curriculum_units' AND column_name='Description') THEN
                        ALTER TABLE curriculum_units ADD COLUMN ""Description"" character varying(500) NULL;
                    END IF;
                END $$;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE curriculum_units DROP COLUMN IF EXISTS ""Description"";");
        }
    }
}
