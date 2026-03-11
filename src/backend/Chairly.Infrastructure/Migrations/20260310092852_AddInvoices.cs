using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chairly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "Invoices" (
                    "Id" uuid NOT NULL,
                    "TenantId" uuid NOT NULL,
                    "BookingId" uuid NOT NULL,
                    "ClientId" uuid NOT NULL,
                    "InvoiceNumber" character varying(20) NOT NULL,
                    "InvoiceDate" date NOT NULL,
                    "TotalAmount" numeric(18,2) NOT NULL,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    "CreatedBy" uuid NOT NULL,
                    "SentAtUtc" timestamp with time zone,
                    "SentBy" uuid,
                    "PaidAtUtc" timestamp with time zone,
                    "PaidBy" uuid,
                    "VoidedAtUtc" timestamp with time zone,
                    "VoidedBy" uuid,
                    CONSTRAINT "PK_Invoices" PRIMARY KEY ("Id")
                );
                """);

            migrationBuilder.Sql("""
                CREATE TABLE IF NOT EXISTS "InvoiceLineItems" (
                    "Id" uuid NOT NULL,
                    "Description" character varying(200) NOT NULL,
                    "Quantity" integer NOT NULL,
                    "UnitPrice" numeric(18,2) NOT NULL,
                    "TotalPrice" numeric(18,2) NOT NULL,
                    "SortOrder" integer NOT NULL,
                    "InvoiceId" uuid NOT NULL,
                    CONSTRAINT "PK_InvoiceLineItems" PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_InvoiceLineItems_Invoices_InvoiceId" FOREIGN KEY ("InvoiceId")
                        REFERENCES "Invoices" ("Id") ON DELETE CASCADE
                );
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_InvoiceLineItems_InvoiceId" ON "InvoiceLineItems" ("InvoiceId");
                """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Invoices_TenantId_BookingId" ON "Invoices" ("TenantId", "BookingId");
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_Invoices_TenantId_ClientId" ON "Invoices" ("TenantId", "ClientId");
                """);

            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS "IX_Invoices_TenantId_CreatedAtUtc" ON "Invoices" ("TenantId", "CreatedAtUtc" DESC);
                """);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Invoices_TenantId_InvoiceNumber" ON "Invoices" ("TenantId", "InvoiceNumber");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceLineItems");

            migrationBuilder.DropTable(
                name: "Invoices");
        }
    }
}
