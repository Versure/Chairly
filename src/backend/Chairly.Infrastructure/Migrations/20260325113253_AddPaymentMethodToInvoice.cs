using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chairly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentMethodToInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  IF NOT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_name = 'Invoices' AND column_name = 'PaymentMethod'
                  ) THEN
                    ALTER TABLE "Invoices" ADD COLUMN "PaymentMethod" character varying(20) NOT NULL DEFAULT 'Pin';
                  END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                UPDATE "Invoices" SET "PaymentMethod" = 'Pin' WHERE "PaymentMethod" = '' OR "PaymentMethod" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Invoices");
        }
    }
}
