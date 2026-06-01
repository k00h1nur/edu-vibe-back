using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BooksTasksAndAssignees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_enrollments_StudentProfileId",
                table: "enrollments",
                newName: "ix_enrollments_student_profile_id");

            migrationBuilder.RenameIndex(
                name: "IX_classes_TeacherUserId",
                table: "classes",
                newName: "ix_classes_teacher_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_attendance_StudentProfileId",
                table: "attendance",
                newName: "ix_attendance_student_profile_id");

            migrationBuilder.RenameIndex(
                name: "IX_attendance_ClassId",
                table: "attendance",
                newName: "ix_attendance_class_id");

            // MaxStudents was added to the Class entity without a migration;
            // catch it up here. Default of 30 (a sensible classroom size) keeps
            // existing rows usable — Class.EnrollStudent throws when
            // activeEnrollments >= MaxStudents, so 0 would lock out every class.
            migrationBuilder.AddColumn<int>(
                name: "MaxStudents",
                table: "classes",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.CreateTable(
                name: "assignment_assignees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assignment_assignees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_assignment_assignees_assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_assignment_assignees_student_profiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "student_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "books",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Author = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Subject = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Level = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CoverImageUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    FileUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_books", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "learning_tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    ContentJson = table.Column<string>(type: "jsonb", nullable: false),
                    SolutionJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_learning_tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_learning_tasks_assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "visitor_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Phone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    Message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Source = table.Column<int>(type: "integer", nullable: false),
                    Course = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    PreferredTime = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_visitor_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "assignment_books",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookId = table.Column<Guid>(type: "uuid", nullable: false),
                    Note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assignment_books", x => x.Id);
                    table.ForeignKey(
                        name: "FK_assignment_books_assignments_AssignmentId",
                        column: x => x.AssignmentId,
                        principalTable: "assignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_assignment_books_books_BookId",
                        column: x => x.BookId,
                        principalTable: "books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "task_submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResponseJson = table.Column<string>(type: "jsonb", nullable: false),
                    Score = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    GradedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GradedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TeacherFeedback = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_submissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_task_submissions_learning_tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "learning_tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_task_submissions_student_profiles_StudentProfileId",
                        column: x => x.StudentProfileId,
                        principalTable: "student_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateIndex(
                name: "ix_users_refresh_token_lookup",
                table: "users",
                columns: new[] { "RefreshTokenHash", "RefreshTokenExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "ix_user_roles_user_id",
                table: "user_roles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_messages_conversation_read_at",
                table: "messages",
                columns: new[] { "ConversationId", "ReadAt" });

            migrationBuilder.CreateIndex(
                name: "ix_conversation_participants_user_id",
                table: "conversation_participants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_attendance_session_id",
                table: "attendance",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_assignment_assignees_AssignmentId_StudentProfileId",
                table: "assignment_assignees",
                columns: new[] { "AssignmentId", "StudentProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_assignment_assignees_student_id",
                table: "assignment_assignees",
                column: "StudentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_assignment_books_AssignmentId_BookId",
                table: "assignment_books",
                columns: new[] { "AssignmentId", "BookId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_assignment_books_BookId",
                table: "assignment_books",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_books_Subject",
                table: "books",
                column: "Subject");

            migrationBuilder.CreateIndex(
                name: "IX_books_Title",
                table: "books",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "ix_learning_tasks_assignment_order",
                table: "learning_tasks",
                columns: new[] { "AssignmentId", "Order" });

            migrationBuilder.CreateIndex(
                name: "ix_task_submissions_student_id",
                table: "task_submissions",
                column: "StudentProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_task_submissions_TaskId_StudentProfileId",
                table: "task_submissions",
                columns: new[] { "TaskId", "StudentProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_visitor_messages_inbox",
                table: "visitor_messages",
                columns: new[] { "IsRead", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assignment_assignees");

            migrationBuilder.DropTable(
                name: "assignment_books");

            migrationBuilder.DropTable(
                name: "task_submissions");

            migrationBuilder.DropTable(
                name: "visitor_messages");

            migrationBuilder.DropTable(
                name: "books");

            migrationBuilder.DropTable(
                name: "learning_tasks");

            migrationBuilder.DropIndex(
                name: "ix_users_refresh_token_lookup",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_user_roles_user_id",
                table: "user_roles");

            migrationBuilder.DropIndex(
                name: "ix_messages_conversation_read_at",
                table: "messages");

            migrationBuilder.DropIndex(
                name: "ix_conversation_participants_user_id",
                table: "conversation_participants");

            migrationBuilder.DropIndex(
                name: "ix_attendance_session_id",
                table: "attendance");

            migrationBuilder.DropColumn(
                name: "MaxStudents",
                table: "classes");

            migrationBuilder.RenameIndex(
                name: "ix_enrollments_student_profile_id",
                table: "enrollments",
                newName: "IX_enrollments_StudentProfileId");

            migrationBuilder.RenameIndex(
                name: "ix_classes_teacher_user_id",
                table: "classes",
                newName: "IX_classes_TeacherUserId");

            migrationBuilder.RenameIndex(
                name: "ix_attendance_student_profile_id",
                table: "attendance",
                newName: "IX_attendance_StudentProfileId");

            migrationBuilder.RenameIndex(
                name: "ix_attendance_class_id",
                table: "attendance",
                newName: "IX_attendance_ClassId");

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442), new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442) });

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442), new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442) });

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442), new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442) });

            migrationBuilder.UpdateData(
                table: "badges",
                keyColumn: "Id",
                keyValue: new Guid("40000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442), new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442), new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442), new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000003"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442), new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000004"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442), new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442), new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442), new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442) });

            migrationBuilder.UpdateData(
                table: "roles",
                keyColumn: "Id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442), new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442) });

            migrationBuilder.UpdateData(
                table: "user_roles",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442), new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442) });

            migrationBuilder.UpdateData(
                table: "user_roles",
                keyColumn: "Id",
                keyValue: new Guid("30000000-0000-0000-0000-000000000002"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442), new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442) });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "Id",
                keyValue: new Guid("20000000-0000-0000-0000-000000000001"),
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442), new DateTime(2026, 5, 18, 6, 2, 30, 335, DateTimeKind.Utc).AddTicks(5442) });
        }
    }
}
