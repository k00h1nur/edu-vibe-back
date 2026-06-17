using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using LMS.Infrastructure.Persistence;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <summary>
    /// telegram_deep_link_tokens — short-lived, one-time tokens that hand a
    /// signed-in web session to the Telegram Mini App (auto-login + account
    /// linking). Raw idempotent SQL in house style; snapshot patched by hand.
    /// </summary>
    [DbContext(typeof(LMSDbContext))]
    [Migration("20260623000000_TelegramDeepLinkTokens")]
    public partial class TelegramDeepLinkTokens : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS telegram_deep_link_tokens (
                    ""Id"" uuid NOT NULL,
                    ""UserId"" uuid NOT NULL,
                    ""Token"" character varying(128) NOT NULL,
                    ""ExpiresAt"" timestamp with time zone NOT NULL,
                    ""ConsumedAt"" timestamp with time zone NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    ""UpdatedAt"" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc'),
                    CONSTRAINT pk_telegram_deep_link_tokens PRIMARY KEY (""Id""),
                    CONSTRAINT fk_telegram_deep_link_tokens_users FOREIGN KEY (""UserId"")
                        REFERENCES users (""Id"") ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX IF NOT EXISTS ix_telegram_deep_link_tokens_token
                    ON telegram_deep_link_tokens (""Token"");
                CREATE INDEX IF NOT EXISTS ix_telegram_deep_link_tokens_expires_at
                    ON telegram_deep_link_tokens (""ExpiresAt"");
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS telegram_deep_link_tokens;");
        }
    }
}
