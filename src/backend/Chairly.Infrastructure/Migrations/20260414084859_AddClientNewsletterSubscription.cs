using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chairly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClientNewsletterSubscription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'Clients' AND column_name = 'IsSubscribedToNewsletter') THEN
                        ALTER TABLE "Clients" ADD COLUMN "IsSubscribedToNewsletter" boolean NOT NULL DEFAULT TRUE;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "NewsletterCampaigns" (
                    "Id" uuid NOT NULL,
                    "TenantId" uuid NOT NULL,
                    "Subject" character varying(500) NOT NULL,
                    "BodyHtml" text NOT NULL,
                    "RecipientFilter" integer NOT NULL,
                    "ScheduledAtUtc" timestamp with time zone NULL,
                    "ScheduledBy" uuid NULL,
                    "QueuedAtUtc" timestamp with time zone NULL,
                    "QueuedBy" uuid NULL,
                    "SentAtUtc" timestamp with time zone NULL,
                    "SentBy" uuid NULL,
                    "CancelledAtUtc" timestamp with time zone NULL,
                    "CancelledBy" uuid NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    "CreatedBy" uuid NOT NULL,
                    "UpdatedAtUtc" timestamp with time zone NULL,
                    "UpdatedBy" uuid NULL,
                    CONSTRAINT "PK_NewsletterCampaigns" PRIMARY KEY ("Id")
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "NewsletterDeliveries" (
                    "Id" uuid NOT NULL,
                    "TenantId" uuid NOT NULL,
                    "CampaignId" uuid NOT NULL,
                    "ClientId" uuid NOT NULL,
                    "Email" character varying(320) NOT NULL,
                    "UnsubscribeToken" character varying(64) NOT NULL,
                    "SentAtUtc" timestamp with time zone NULL,
                    "FailedAtUtc" timestamp with time zone NULL,
                    "FailureReason" character varying(1000) NULL,
                    "UnsubscribedAtUtc" timestamp with time zone NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    "CreatedBy" uuid NOT NULL,
                    CONSTRAINT "PK_NewsletterDeliveries" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_NewsletterDeliveries_Clients_ClientId" FOREIGN KEY ("ClientId") REFERENCES "Clients" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_NewsletterDeliveries_NewsletterCampaigns_CampaignId" FOREIGN KEY ("CampaignId") REFERENCES "NewsletterCampaigns" ("Id") ON DELETE CASCADE
                );
                """);

            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_NewsletterCampaigns_TenantId_CreatedAtUtc" ON "NewsletterCampaigns" ("TenantId", "CreatedAtUtc");""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_NewsletterCampaigns_TenantId_ScheduledAtUtc" ON "NewsletterCampaigns" ("TenantId", "ScheduledAtUtc");""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_NewsletterDeliveries_CampaignId" ON "NewsletterDeliveries" ("CampaignId");""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_NewsletterDeliveries_ClientId" ON "NewsletterDeliveries" ("ClientId");""");
            migrationBuilder.Sql("""CREATE INDEX IF NOT EXISTS "IX_NewsletterDeliveries_TenantId_CampaignId" ON "NewsletterDeliveries" ("TenantId", "CampaignId");""");
            migrationBuilder.Sql("""CREATE UNIQUE INDEX IF NOT EXISTS "IX_NewsletterDeliveries_UnsubscribeToken" ON "NewsletterDeliveries" ("UnsubscribeToken");""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            ArgumentNullException.ThrowIfNull(migrationBuilder);

            migrationBuilder.DropTable(name: "NewsletterDeliveries");
            migrationBuilder.DropTable(name: "NewsletterCampaigns");
            migrationBuilder.DropColumn(name: "IsSubscribedToNewsletter", table: "Clients");
        }
    }
}
