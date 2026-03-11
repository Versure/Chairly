using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chairly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "ServiceCategories" (
                    "Id" uuid NOT NULL,
                    "TenantId" uuid NOT NULL,
                    "Name" character varying(100) NOT NULL,
                    "SortOrder" integer NOT NULL,
                    CONSTRAINT "PK_ServiceCategories" PRIMARY KEY ("Id")
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "Services" (
                    "Id" uuid NOT NULL,
                    "TenantId" uuid NOT NULL,
                    "Name" character varying(150) NOT NULL,
                    "Description" text,
                    "Duration" interval NOT NULL,
                    "Price" numeric(10,2) NOT NULL,
                    "CategoryId" uuid,
                    "IsActive" boolean NOT NULL,
                    "SortOrder" integer NOT NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    "UpdatedAtUtc" timestamp with time zone,
                    CONSTRAINT "PK_Services" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_Services_ServiceCategories_CategoryId" FOREIGN KEY ("CategoryId")
                        REFERENCES "ServiceCategories" ("Id") ON DELETE SET NULL
                );
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_Services_CategoryId" ON "Services" ("CategoryId");
                """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Services_Name_TenantId" ON "Services" ("Name", "TenantId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropTable(
                name: "ServiceCategories");
        }
    }
}
