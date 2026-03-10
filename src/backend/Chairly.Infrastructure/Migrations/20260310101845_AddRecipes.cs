using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chairly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use raw SQL with IF NOT EXISTS to be idempotent — on databases where
            // the old AddInvoices migration already created these tables, plain
            // CREATE TABLE would fail with "relation already exists".
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "Recipes" (
                    "Id" uuid NOT NULL,
                    "TenantId" uuid NOT NULL,
                    "BookingId" uuid NOT NULL,
                    "ClientId" uuid NOT NULL,
                    "StaffMemberId" uuid NOT NULL,
                    "Title" character varying(200) NOT NULL,
                    "Notes" character varying(2000),
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    "CreatedBy" uuid NOT NULL,
                    "UpdatedAtUtc" timestamp with time zone,
                    "UpdatedBy" uuid,
                    CONSTRAINT "PK_Recipes" PRIMARY KEY ("Id")
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "RecipeProducts" (
                    "Id" uuid NOT NULL,
                    "Name" character varying(100) NOT NULL,
                    "Brand" character varying(100),
                    "Quantity" character varying(50),
                    "SortOrder" integer NOT NULL,
                    "RecipeId" uuid NOT NULL,
                    CONSTRAINT "PK_RecipeProducts" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_RecipeProducts_Recipes_RecipeId" FOREIGN KEY ("RecipeId")
                        REFERENCES "Recipes" ("Id") ON DELETE CASCADE
                );
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_RecipeProducts_RecipeId"
                    ON "RecipeProducts" ("RecipeId");
                """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Recipes_TenantId_BookingId"
                    ON "Recipes" ("TenantId", "BookingId");
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_Recipes_TenantId_ClientId"
                    ON "Recipes" ("TenantId", "ClientId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecipeProducts");

            migrationBuilder.DropTable(
                name: "Recipes");
        }
    }
}
