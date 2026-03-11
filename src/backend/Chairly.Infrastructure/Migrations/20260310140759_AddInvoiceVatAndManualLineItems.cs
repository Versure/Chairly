using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chairly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceVatAndManualLineItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'Invoices' AND column_name = 'SubTotalAmount'
                    ) THEN
                        ALTER TABLE "Invoices" ADD COLUMN "SubTotalAmount" numeric(18,2) NOT NULL DEFAULT 0;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'Invoices' AND column_name = 'TotalVatAmount'
                    ) THEN
                        ALTER TABLE "Invoices" ADD COLUMN "TotalVatAmount" numeric(18,2) NOT NULL DEFAULT 0;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'InvoiceLineItems' AND column_name = 'IsManual'
                    ) THEN
                        ALTER TABLE "InvoiceLineItems" ADD COLUMN "IsManual" boolean NOT NULL DEFAULT false;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'InvoiceLineItems' AND column_name = 'VatAmount'
                    ) THEN
                        ALTER TABLE "InvoiceLineItems" ADD COLUMN "VatAmount" numeric(18,2) NOT NULL DEFAULT 0;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'InvoiceLineItems' AND column_name = 'VatPercentage'
                    ) THEN
                        ALTER TABLE "InvoiceLineItems" ADD COLUMN "VatPercentage" numeric(5,2) NOT NULL DEFAULT 0;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubTotalAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TotalVatAmount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IsManual",
                table: "InvoiceLineItems");

            migrationBuilder.DropColumn(
                name: "VatAmount",
                table: "InvoiceLineItems");

            migrationBuilder.DropColumn(
                name: "VatPercentage",
                table: "InvoiceLineItems");
        }
    }
}
