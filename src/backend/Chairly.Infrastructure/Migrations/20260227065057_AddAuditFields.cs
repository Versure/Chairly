using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chairly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'Services' AND column_name = 'CreatedBy'
                    ) THEN
                        ALTER TABLE "Services" ADD COLUMN "CreatedBy" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'Services' AND column_name = 'UpdatedBy'
                    ) THEN
                        ALTER TABLE "Services" ADD COLUMN "UpdatedBy" uuid;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'ServiceCategories' AND column_name = 'CreatedAtUtc'
                    ) THEN
                        ALTER TABLE "ServiceCategories" ADD COLUMN "CreatedAtUtc" timestamp with time zone NOT NULL DEFAULT '0001-01-01 00:00:00+00';
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name = 'ServiceCategories' AND column_name = 'CreatedBy'
                    ) THEN
                        ALTER TABLE "ServiceCategories" ADD COLUMN "CreatedBy" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "ServiceCategories");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ServiceCategories");
        }
    }
}
