using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chairly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "TenantSettings" (
                    "Id" uuid NOT NULL,
                    "TenantId" uuid NOT NULL,
                    "CompanyName" character varying(200) NULL,
                    "CompanyEmail" character varying(200) NULL,
                    "CompanyAddress" character varying(500) NULL,
                    "CompanyPhone" character varying(50) NULL,
                    "IbanNumber" character varying(34) NULL,
                    "VatNumber" character varying(50) NULL,
                    "PaymentPeriodDays" integer NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    "CreatedBy" uuid NOT NULL,
                    "UpdatedAtUtc" timestamp with time zone NULL,
                    "UpdatedBy" uuid NULL,
                    CONSTRAINT "PK_TenantSettings" PRIMARY KEY ("Id")
                );
                """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_TenantSettings_TenantId" ON "TenantSettings" ("TenantId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantSettings");
        }
    }
}
