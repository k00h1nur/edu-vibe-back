using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// Adds the optional Description (instructions) column to assignments.
    /// Raw idempotent SQL in house style; [Migration] on the class, snapshot
    /// patched by hand.
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260621000000_AssignmentDescription")]
    public partial class AssignmentDescription : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE assignments
                    ADD COLUMN IF NOT EXISTS ""Description"" character varying(4000) NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE assignments DROP COLUMN IF EXISTS ""Description"";");
        }
    }
}
