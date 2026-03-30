using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chairly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateEmailTemplateToSingleBody : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Body column if it doesn't exist
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'EmailTemplates' AND column_name = 'Body') THEN
                        ALTER TABLE "EmailTemplates" ADD COLUMN "Body" character varying(10000) NOT NULL DEFAULT '';
                    END IF;
                END $$;
                """);

            // Migrate existing data: concatenate MainMessage and ClosingMessage into Body
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'EmailTemplates' AND column_name = 'MainMessage') THEN
                        UPDATE "EmailTemplates" SET "Body" = COALESCE("MainMessage", '') || COALESCE("ClosingMessage", '') WHERE "Body" = '';
                    END IF;
                END $$;
                """);

            // Drop old columns if they exist
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'EmailTemplates' AND column_name = 'MainMessage') THEN
                        ALTER TABLE "EmailTemplates" DROP COLUMN "MainMessage";
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'EmailTemplates' AND column_name = 'ClosingMessage') THEN
                        ALTER TABLE "EmailTemplates" DROP COLUMN "ClosingMessage";
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'EmailTemplates' AND column_name = 'DateLabel') THEN
                        ALTER TABLE "EmailTemplates" DROP COLUMN "DateLabel";
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'EmailTemplates' AND column_name = 'ServicesLabel') THEN
                        ALTER TABLE "EmailTemplates" DROP COLUMN "ServicesLabel";
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Body",
                table: "EmailTemplates");

            migrationBuilder.AddColumn<string>(
                name: "ClosingMessage",
                table: "EmailTemplates",
                type: "character varying(3000)",
                maxLength: 3000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DateLabel",
                table: "EmailTemplates",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MainMessage",
                table: "EmailTemplates",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ServicesLabel",
                table: "EmailTemplates",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
