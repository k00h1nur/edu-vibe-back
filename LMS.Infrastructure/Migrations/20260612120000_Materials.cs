using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// Course materials feature — creates the <c>materials</c> +
    /// <c>material_classes</c> tables that back the LMS admin Materials
    /// page.
    ///
    /// Written with raw idempotent SQL (rather than the EF builder) so it
    /// survives:
    ///   • a fresh DB — both tables created cleanly;
    ///   • a partially-applied DB — IF NOT EXISTS makes it a no-op;
    ///   • the existing snapshot drift in this project — see the
    ///     <c>AvatarUrlAndAdminFieldsFix</c> precedent.
    /// </summary>
    public partial class Materials : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS materials (
                    ""Id"" uuid NOT NULL,
                    ""Title"" character varying(256) NOT NULL,
                    ""Description"" character varying(2000) NULL,
                    ""Visibility"" integer NOT NULL,
                    ""StoredFileName"" character varying(256) NOT NULL,
                    ""OriginalFileName"" character varying(256) NOT NULL,
                    ""MimeType"" character varying(128) NOT NULL,
                    ""FileSize"" bigint NOT NULL,
                    ""UploadedByUserId"" uuid NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_materials PRIMARY KEY (""Id""),
                    CONSTRAINT fk_materials_users_uploaded_by FOREIGN KEY (""UploadedByUserId"")
                        REFERENCES users (""Id"") ON DELETE RESTRICT
                );
                CREATE INDEX IF NOT EXISTS ix_materials_created_at ON materials (""CreatedAt"");
                CREATE INDEX IF NOT EXISTS ix_materials_uploaded_by ON materials (""UploadedByUserId"");
                CREATE INDEX IF NOT EXISTS ix_materials_visibility ON materials (""Visibility"");
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS material_classes (
                    ""Id"" uuid NOT NULL,
                    ""MaterialId"" uuid NOT NULL,
                    ""ClassId"" uuid NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_material_classes PRIMARY KEY (""Id""),
                    CONSTRAINT fk_material_classes_materials FOREIGN KEY (""MaterialId"")
                        REFERENCES materials (""Id"") ON DELETE CASCADE,
                    CONSTRAINT fk_material_classes_classes FOREIGN KEY (""ClassId"")
                        REFERENCES classes (""Id"") ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ix_material_classes_material_class
                    ON material_classes (""MaterialId"", ""ClassId"");
                CREATE INDEX IF NOT EXISTS ix_material_classes_class_id ON material_classes (""ClassId"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS material_classes;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS materials;");
        }
    }
}
