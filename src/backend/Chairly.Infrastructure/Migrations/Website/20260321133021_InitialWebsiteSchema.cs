using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chairly.Infrastructure.Migrations.Website
{
    /// <inheritdoc />
    public partial class InitialWebsiteSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "DemoRequests" (
                    "Id" uuid NOT NULL,
                    "ContactName" character varying(200) NOT NULL,
                    "SalonName" character varying(200) NOT NULL,
                    "Email" character varying(256) NOT NULL,
                    "PhoneNumber" character varying(50),
                    "Message" character varying(2000),
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    "CreatedBy" uuid,
                    "ReviewedAtUtc" timestamp with time zone,
                    "ReviewedBy" uuid,
                    CONSTRAINT "PK_DemoRequests" PRIMARY KEY ("Id")
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "SignUpRequests" (
                    "Id" uuid NOT NULL,
                    "SalonName" character varying(200) NOT NULL,
                    "OwnerFirstName" character varying(100) NOT NULL,
                    "OwnerLastName" character varying(100) NOT NULL,
                    "Email" character varying(256) NOT NULL,
                    "PhoneNumber" character varying(50),
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    "CreatedBy" uuid,
                    "ProvisionedAtUtc" timestamp with time zone,
                    "ProvisionedBy" uuid,
                    "RejectedAtUtc" timestamp with time zone,
                    "RejectedBy" uuid,
                    "RejectionReason" character varying(1000),
                    CONSTRAINT "PK_SignUpRequests" PRIMARY KEY ("Id")
                );
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_DemoRequests_CreatedAtUtc" ON "DemoRequests" ("CreatedAtUtc");
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_SignUpRequests_CreatedAtUtc" ON "SignUpRequests" ("CreatedAtUtc");
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_SignUpRequests_Email" ON "SignUpRequests" ("Email");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemoRequests");

            migrationBuilder.DropTable(
                name: "SignUpRequests");
        }
    }
}
