using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// Office Info: an editable Google Maps embed URL (the iframe src), rendered
    /// on the marketing site's Contact map. Additive + idempotent — nullable
    /// column, no existing data touched.
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260624000000_OfficeInfoMapEmbedUrl")]
    public partial class OfficeInfoMapEmbedUrl : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns
                        WHERE table_name='office_info' AND column_name='MapEmbedUrl') THEN
                        ALTER TABLE office_info ADD COLUMN ""MapEmbedUrl"" character varying(2000) NULL;
                    END IF;
                END $$;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE office_info DROP COLUMN IF EXISTS ""MapEmbedUrl"";");
        }
    }
}
