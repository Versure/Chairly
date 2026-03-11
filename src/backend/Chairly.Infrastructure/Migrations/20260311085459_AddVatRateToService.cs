using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chairly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVatRateToService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'Services' AND column_name = 'VatRate'
                    ) THEN
                        ALTER TABLE "Services" ADD COLUMN "VatRate" numeric(5,2);
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "Services");
        }
    }
}
