using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chairly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitCompanyAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'TenantSettings' AND column_name = 'CompanyAddress') THEN
                        ALTER TABLE "TenantSettings" DROP COLUMN "CompanyAddress";
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'TenantSettings' AND column_name = 'Street') THEN
                        ALTER TABLE "TenantSettings" ADD COLUMN "Street" character varying(200) NULL;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'TenantSettings' AND column_name = 'HouseNumber') THEN
                        ALTER TABLE "TenantSettings" ADD COLUMN "HouseNumber" character varying(20) NULL;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'TenantSettings' AND column_name = 'PostalCode') THEN
                        ALTER TABLE "TenantSettings" ADD COLUMN "PostalCode" character varying(20) NULL;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'TenantSettings' AND column_name = 'City') THEN
                        ALTER TABLE "TenantSettings" ADD COLUMN "City" character varying(100) NULL;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "HouseNumber",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "TenantSettings");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "TenantSettings");

            migrationBuilder.AddColumn<string>(
                name: "CompanyAddress",
                table: "TenantSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
