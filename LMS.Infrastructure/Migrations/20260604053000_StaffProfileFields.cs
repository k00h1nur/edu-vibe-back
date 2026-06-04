using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// Adds optional profile fields (first name, last name, phone, description)
    /// to <c>staff_profiles</c> — mirror of the StudentProfileFields migration.
    /// Every column is nullable so existing rows stay valid after applying.
    /// </summary>
    public partial class StaffProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "staff_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "staff_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "staff_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "staff_profiles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "staff_profiles");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "staff_profiles");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "staff_profiles");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "staff_profiles");
        }
    }
}
