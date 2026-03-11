using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chairly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "Clients" (
                    "Id" uuid NOT NULL,
                    "TenantId" uuid NOT NULL,
                    "FirstName" character varying(100) NOT NULL,
                    "LastName" character varying(100) NOT NULL,
                    "Email" character varying(256),
                    "PhoneNumber" character varying(50),
                    "Notes" character varying(1000),
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    "CreatedBy" uuid NOT NULL,
                    "UpdatedAtUtc" timestamp with time zone,
                    "UpdatedBy" uuid,
                    "DeletedAtUtc" timestamp with time zone,
                    "DeletedBy" uuid,
                    CONSTRAINT "PK_Clients" PRIMARY KEY ("Id")
                );
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_Clients_TenantId" ON "Clients" ("TenantId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Clients");
        }
    }
}
