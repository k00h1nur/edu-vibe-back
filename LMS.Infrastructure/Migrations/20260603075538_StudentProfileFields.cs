using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StudentProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "student_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "student_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "student_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "student_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199), new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199) });

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199), new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199) });

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199), new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199) });

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199), new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199), new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199), new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199), new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199), new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199), new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199), new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199), new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199) });

            migrationBuilder.UpdateData(
                table: "user_roles",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199), new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199) });

            migrationBuilder.UpdateData(
                table: "user_roles",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199), new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199) });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199), new DateTime(2026, 6, 3, 7, 55, 38, 424, DateTimeKind.Utc).AddTicks(3199) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "student_profiles");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "student_profiles");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "student_profiles");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "student_profiles");

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880), new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880) });

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880), new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880) });

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880), new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880) });

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880), new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880), new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880), new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880), new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880), new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880), new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880), new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880), new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880) });

            migrationBuilder.UpdateData(
                table: "user_roles",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880), new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880) });

            migrationBuilder.UpdateData(
                table: "user_roles",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880), new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880) });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880), new DateTime(2026, 6, 1, 11, 6, 9, 798, DateTimeKind.Utc).AddTicks(3880) });
        }
    }
}
