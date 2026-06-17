using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// telegram_accounts — links a platform user (1:1) to their verified Telegram
    /// identity, created when a user authenticates through the Telegram Mini App
    /// or links from the web. Raw idempotent SQL in house style; [Migration] on
    /// the class, snapshot patched by hand.
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260620000000_TelegramAccounts")]
    public partial class TelegramAccounts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS telegram_accounts (
                    ""Id"" uuid NOT NULL,
                    ""UserId"" uuid NOT NULL,
                    ""TelegramUserId"" bigint NOT NULL,
                    ""Username"" character varying(64) NULL,
                    ""FirstName"" character varying(128) NULL,
                    ""LastName"" character varying(128) NULL,
                    ""PhotoUrl"" character varying(1024) NULL,
                    ""LinkedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_telegram_accounts PRIMARY KEY (""Id""),
                    CONSTRAINT fk_telegram_accounts_users FOREIGN KEY (""UserId"")
                        REFERENCES users (""Id"") ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ix_telegram_accounts_telegram_user_id
                    ON telegram_accounts (""TelegramUserId"");
                CREATE UNIQUE INDEX IF NOT EXISTS ix_telegram_accounts_user_id
                    ON telegram_accounts (""UserId"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS telegram_accounts;");
        }
    }
}
