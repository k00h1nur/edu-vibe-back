using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// Class-level content hub — class_resources holds the shared "course setup"
    /// an admin (or the class teacher) attaches to a whole class: course roadmap,
    /// video lessons, reference links and default homework. Class-wide and
    /// link/text based — distinct from lesson_materials (files on one lesson) and
    /// assignments (graded submissions). Raw idempotent SQL in house style;
    /// [Migration] on the class, snapshot patched by hand.
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260618000000_ClassResourceLayer")]
    public partial class ClassResourceLayer : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS class_resources (
                    ""Id"" uuid NOT NULL,
                    ""ClassId"" uuid NOT NULL,
                    ""Kind"" integer NOT NULL,
                    ""Title"" character varying(256) NOT NULL,
                    ""Url"" text NULL,
                    ""Content"" text NULL,
                    ""Position"" integer NOT NULL DEFAULT 0,
                    ""CreatedByUserId"" uuid NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_class_resources PRIMARY KEY (""Id""),
                    CONSTRAINT fk_class_resources_classes FOREIGN KEY (""ClassId"")
                        REFERENCES classes (""Id"") ON DELETE CASCADE
                );
                CREATE INDEX IF NOT EXISTS ix_class_resources_class_id ON class_resources (""ClassId"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS class_resources;");
        }
    }
}
