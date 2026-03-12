using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chairly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "Notifications" (
                    "Id" uuid NOT NULL,
                    "TenantId" uuid NOT NULL,
                    "RecipientId" uuid NOT NULL,
                    "RecipientType" integer NOT NULL,
                    "Channel" integer NOT NULL,
                    "Type" integer NOT NULL,
                    "ReferenceId" uuid NOT NULL,
                    "ScheduledAtUtc" timestamp with time zone NOT NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    "CreatedBy" uuid NOT NULL,
                    "UpdatedAtUtc" timestamp with time zone,
                    "UpdatedBy" uuid,
                    "SentAtUtc" timestamp with time zone,
                    "FailedAtUtc" timestamp with time zone,
                    "FailureReason" character varying(1000),
                    "RetryCount" integer NOT NULL DEFAULT 0,
                    CONSTRAINT "PK_Notifications" PRIMARY KEY ("Id")
                );
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_Notifications_TenantId_ScheduledAtUtc" ON "Notifications" ("TenantId", "ScheduledAtUtc");
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_Notifications_TenantId_ReferenceId" ON "Notifications" ("TenantId", "ReferenceId");
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_Notifications_TenantId_CreatedAtUtc" ON "Notifications" ("TenantId", "CreatedAtUtc" DESC);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notifications");
        }
    }
}
