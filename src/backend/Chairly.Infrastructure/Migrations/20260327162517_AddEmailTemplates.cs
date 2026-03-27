using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chairly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "EmailTemplates" (
                    "Id" uuid NOT NULL,
                    "TenantId" uuid NOT NULL,
                    "TemplateType" integer NOT NULL,
                    "Subject" character varying(500) NOT NULL,
                    "MainMessage" character varying(2000) NOT NULL,
                    "ClosingMessage" character varying(1000) NOT NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    "CreatedBy" uuid NOT NULL,
                    "UpdatedAtUtc" timestamp with time zone,
                    "UpdatedBy" uuid,
                    CONSTRAINT "PK_EmailTemplates" PRIMARY KEY ("Id")
                );
                """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_EmailTemplates_TenantId_TemplateType"
                    ON "EmailTemplates" ("TenantId", "TemplateType");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailTemplates");
        }
    }
}
